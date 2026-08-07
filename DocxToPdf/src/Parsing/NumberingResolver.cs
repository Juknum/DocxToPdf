using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Resolves document numbering definitions (bullets, numbered lists, multi-level counters) from OpenXML numbering parts.
	/// </summary>
	public class NumberingResolver {
		private readonly Dictionary<int, int> _numIdToAbstractNumId = new();
		private readonly Dictionary<int, AbstractNum> _abstractNumById = new();
		private readonly Dictionary<string, int> _counters = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="NumberingResolver"/> class.
		/// </summary>
		/// <param name="wordDoc">The WordprocessingDocument package. Cannot be null.</param>
		public NumberingResolver(WordprocessingDocument wordDoc) {
			if (wordDoc == null) throw new ArgumentNullException(nameof(wordDoc));

			NumberingDefinitionsPart? numberingPart = wordDoc.MainDocumentPart?.NumberingDefinitionsPart;
			if (numberingPart?.Numbering != null) {
				Numbering numbering = numberingPart.Numbering;

				foreach (AbstractNum abstractNum in numbering.Elements<AbstractNum>()) {
					if (abstractNum.AbstractNumberId?.Value != null) {
						_abstractNumById[abstractNum.AbstractNumberId.Value] = abstractNum;
					}
				}

				foreach (NumberingInstance numInstance in numbering.Elements<NumberingInstance>()) {
					if (numInstance.NumberID?.Value != null && numInstance.AbstractNumId?.Val?.Value != null) {
						_numIdToAbstractNumId[numInstance.NumberID.Value] = numInstance.AbstractNumId.Val.Value;
					}
				}
			}
		}

		/// <summary>
		/// Resolves paragraph numbering properties into a <see cref="ListFormatModel"/>.
		/// </summary>
		/// <param name="numPr">The OpenXML NumberingProperties element.</param>
		/// <returns>A populated <see cref="ListFormatModel"/> or null if numbering properties are missing.</returns>
		public ListFormatModel? ResolveListFormat(NumberingProperties? numPr) {
			if (numPr?.NumberingId?.Val?.Value == null || numPr.NumberingLevelReference?.Val?.Value == null) {
				return null;
			}

			int numId = numPr.NumberingId.Val.Value;
			int levelIndex = numPr.NumberingLevelReference.Val.Value;

			if (!_numIdToAbstractNumId.TryGetValue(numId, out int abstractNumId) ||
				!_abstractNumById.TryGetValue(abstractNumId, out AbstractNum? abstractNum)) {
				return null;
			}

			Level? level = GetLevel(abstractNum, levelIndex);
			if (level == null) {
				return null;
			}

			ListType listType = ListType.Bullet;
			string numFmt = level.NumberingFormat?.Val?.InnerText?.ToLowerInvariant() 
				?? (level.NumberingFormat?.Val?.HasValue == true ? level.NumberingFormat.Val.Value.ToString().ToLowerInvariant() : "bullet");

			if (numFmt != "bullet" && numFmt != "none") {
				listType = ListType.Numbered;
			}

			string counterKey = $"{numId}_{levelIndex}";
			if (!_counters.ContainsKey(counterKey)) {
				int startVal = 1;
				if (level.StartNumberingValue?.Val?.Value != null) {
					startVal = level.StartNumberingValue.Val.Value;
				}
				_counters[counterKey] = startVal;
			} else {
				_counters[counterKey]++;
			}

			int count = _counters[counterKey];
			string markerText = FormatMarkerText(level.LevelText?.Val?.Value, numFmt, count);

			double leftIndentPt = 0;
			double hangingIndentPt = 0;

			if (level.PreviousParagraphProperties?.Indentation != null) {
				var ind = level.PreviousParagraphProperties.Indentation;
				if (ind.Left != null && double.TryParse(ind.Left.Value, out double leftTwips)) {
					leftIndentPt = TwipConverter.TwipsToPoints(leftTwips);
				}
				if (ind.Hanging != null && double.TryParse(ind.Hanging.Value, out double hangingTwips)) {
					hangingIndentPt = TwipConverter.TwipsToPoints(hangingTwips);
				}
			}

			return new ListFormatModel {
				NumberingId = numId,
				Level = levelIndex,
				Type = listType,
				MarkerText = markerText,
				LeftIndentPt = leftIndentPt,
				HangingIndentPt = hangingIndentPt
			};
		}

		private Level? GetLevel(AbstractNum abstractNum, int levelIndex) {
			foreach (Level lvl in abstractNum.Elements<Level>()) {
				if (lvl.LevelIndex?.Value == levelIndex) {
					return lvl;
				}
			}
			return null;
		}

		private string FormatMarkerText(string? lvlTextPattern, string numFmt, int count) {
			if (numFmt == "bullet" || string.IsNullOrEmpty(lvlTextPattern)) {
				return GetBulletSymbol(lvlTextPattern);
			}

			string formattedNumber = FormatNumber(numFmt, count);
			if (string.IsNullOrEmpty(lvlTextPattern)) {
				return formattedNumber + ".";
			}

			// Replace %1, %2, etc. in lvlTextPattern with formatted number
			string result = lvlTextPattern!;
			result = result.Replace("%1", formattedNumber)
						   .Replace("%2", formattedNumber)
						   .Replace("%3", formattedNumber);
			return result;
		}

		private string GetBulletSymbol(string? lvlText) {
			if (string.IsNullOrEmpty(lvlText)) return "•";
			if (lvlText == "o") return "◦";
			if (lvlText == "§" || lvlText == "v") return "▪";
			return "•";
		}

		private string FormatNumber(string numFmt, int value) {
			if (numFmt.Contains("lowerroman") || numFmt.Contains("lower_roman")) return ToRoman(value).ToLower();
			if (numFmt.Contains("upperroman") || numFmt.Contains("upper_roman")) return ToRoman(value).ToUpper();
			if (numFmt.Contains("lowerletter") || numFmt.Contains("lower_letter")) return ToLetter(value).ToLower();
			if (numFmt.Contains("upperletter") || numFmt.Contains("upper_letter")) return ToLetter(value).ToUpper();
			return value.ToString();
		}

		private string ToRoman(int number) {
			if (number <= 0) return number.ToString();
			if (number >= 1000) return "M" + ToRoman(number - 1000);
			if (number >= 900) return "CM" + ToRoman(number - 900);
			if (number >= 500) return "D" + ToRoman(number - 500);
			if (number >= 400) return "CD" + ToRoman(number - 400);
			if (number >= 100) return "C" + ToRoman(number - 100);
			if (number >= 90) return "XC" + ToRoman(number - 90);
			if (number >= 50) return "L" + ToRoman(number - 50);
			if (number >= 40) return "XL" + ToRoman(number - 40);
			if (number >= 10) return "X" + ToRoman(number - 10);
			if (number >= 9) return "IX";
			if (number >= 5) return "V";
			if (number >= 4) return "IV";
			if (number >= 1) return "I" + ToRoman(number - 1);
			return string.Empty;
		}

		private string ToLetter(int number) {
			if (number <= 0) return "a";
			int index = (number - 1) % 26;
			char letter = (char)('a' + index);
			return letter.ToString();
		}
	}
}
