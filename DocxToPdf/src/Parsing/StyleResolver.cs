using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Constants;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Represents resolved run formatting styles including font family, size, colors, and decorations.
	/// </summary>
	public class ResolvedRunStyle {
		/// <summary>Gets or sets font family name.</summary>
		public string FontFamily { get; set; } = FontConstants.DefaultFontFamily;
		/// <summary>Gets or sets font size in points.</summary>
		public double FontSizePt { get; set; } = 11.0;
		/// <summary>Gets or sets whether text is bold.</summary>
		public bool IsBold { get; set; }
		/// <summary>Gets or sets whether text is italic.</summary>
		public bool IsItalic { get; set; }
		/// <summary>Gets or sets whether text has underline decoration.</summary>
		public bool IsUnderline { get; set; }
		/// <summary>Gets or sets whether text has strikethrough decoration.</summary>
		public bool IsStrikeThrough { get; set; }
		/// <summary>Gets or sets text foreground color in HEX format.</summary>
		public string TextColorHex { get; set; } = ColorConstants.DefaultBlackHex;
		/// <summary>Gets or sets text background shading color in HEX format.</summary>
		public string? BackgroundColorHex { get; set; }
	}

	/// <summary>
	/// Represents resolved paragraph formatting styles including alignment, line spacing, and indentations.
	/// </summary>
	public class ResolvedParagraphStyle {
		/// <summary>Gets or sets paragraph text alignment.</summary>
		public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;
		/// <summary>Gets or sets spacing before paragraph in points.</summary>
		public double SpacingBeforePt { get; set; }
		/// <summary>Gets or sets spacing after paragraph in points.</summary>
		public double SpacingAfterPt { get; set; }
		/// <summary>Gets or sets line height in points.</summary>
		public double LineHeightPt { get; set; }
		/// <summary>Gets or sets whether line height is specified as a multiple.</summary>
		public bool IsLineHeightMultiple { get; set; }
		/// <summary>Gets or sets left margin indentation in points.</summary>
		public double LeftIndentPt { get; set; }
		/// <summary>Gets or sets right margin indentation in points.</summary>
		public double RightIndentPt { get; set; }
		/// <summary>Gets or sets first line indentation in points.</summary>
		public double FirstLineIndentPt { get; set; }
		/// <summary>Gets or sets hanging indentation in points.</summary>
		public double HangingIndentPt { get; set; }
	}

	/// <summary>
	/// Resolves paragraph and run styles across document defaults, named styles, and direct formatting.
	/// </summary>
	public class StyleResolver : IStyleResolver {
		private readonly Dictionary<string, Style> _stylesById = new(StringComparer.OrdinalIgnoreCase);
		private RunPropertiesDefault? _defaultRunProperties;
		private ParagraphPropertiesDefault? _defaultParagraphProperties;

		/// <summary>
		/// Initializes a new instance of the <see cref="StyleResolver"/> class.
		/// </summary>
		/// <param name="wordDoc">The WordprocessingDocument package. Cannot be null.</param>
		public StyleResolver(WordprocessingDocument wordDoc) {
			if (wordDoc == null) throw new ArgumentNullException(nameof(wordDoc));

			StyleDefinitionsPart? stylesPart = wordDoc.MainDocumentPart?.StyleDefinitionsPart;
			if (stylesPart?.Styles != null) {
				Styles styles = stylesPart.Styles;
				
				DocDefaults? docDefaults = styles.DocDefaults;
				if (docDefaults != null) {
					_defaultRunProperties = docDefaults.RunPropertiesDefault;
					_defaultParagraphProperties = docDefaults.ParagraphPropertiesDefault;
				}

				foreach (Style style in styles.Elements<Style>()) {
					if (style.StyleId != null) {
						_stylesById[style.StyleId.Value!] = style;
					}
				}
			}
		}

		/// <inheritdoc />
		public ResolvedParagraphStyle ResolveParagraphStyle(ParagraphProperties? pPr, string? paragraphStyleId) {
			ResolvedParagraphStyle result = new();

			// 1. DocDefaults
			if (_defaultParagraphProperties?.ParagraphPropertiesBaseStyle != null) {
				ApplyParagraphProperties(result, _defaultParagraphProperties.ParagraphPropertiesBaseStyle);
			}

			// 2. Named Paragraph Style (and its basedOn chain)
			if (!string.IsNullOrEmpty(paragraphStyleId)) {
				ApplyParagraphStyleRecursive(result, paragraphStyleId!);
			}

			// 3. Direct Paragraph Formatting (w:pPr)
			if (pPr != null) {
				ApplyParagraphProperties(result, pPr);
			}

			return result;
		}

		/// <inheritdoc />
		public ResolvedRunStyle ResolveRunStyle(RunProperties? rPr, string? runStyleId, ParagraphProperties? pPr, string? paragraphStyleId) {
			ResolvedRunStyle result = new();

			// 1. DocDefaults
			if (_defaultRunProperties?.RunPropertiesBaseStyle != null) {
				ApplyRunProperties(result, _defaultRunProperties.RunPropertiesBaseStyle);
			}

			// 2. Paragraph Style run properties (and its basedOn chain)
			if (!string.IsNullOrEmpty(paragraphStyleId)) {
				ApplyParagraphStyleRunPropertiesRecursive(result, paragraphStyleId!);
			}

			// 3. Direct Paragraph run properties
			if (pPr != null) {
				ParagraphMarkRunProperties? pMarkRPr = pPr.GetFirstChild<ParagraphMarkRunProperties>();
				if (pMarkRPr != null) {
					ApplyRunProperties(result, pMarkRPr);
				}
			}

			// 4. Character Style (and its basedOn chain)
			if (!string.IsNullOrEmpty(runStyleId)) {
				ApplyRunStyleRecursive(result, runStyleId!);
			}

			// 5. Direct Run Formatting (w:rPr)
			if (rPr != null) {
				ApplyRunProperties(result, rPr);
			}

			return result;
		}

		private void ApplyParagraphStyleRecursive(ResolvedParagraphStyle target, string styleId) {
			if (_stylesById.TryGetValue(styleId, out Style? style)) {
				if (style.BasedOn?.Val != null) {
					ApplyParagraphStyleRecursive(target, style.BasedOn.Val.Value!);
				}
				if (style.StyleParagraphProperties != null) {
					ApplyParagraphProperties(target, style.StyleParagraphProperties);
				}
			}
		}

		private void ApplyParagraphStyleRunPropertiesRecursive(ResolvedRunStyle target, string styleId) {
			if (_stylesById.TryGetValue(styleId, out Style? style)) {
				if (style.BasedOn?.Val != null) {
					ApplyParagraphStyleRunPropertiesRecursive(target, style.BasedOn.Val.Value!);
				}
				if (style.StyleRunProperties != null) {
					ApplyRunProperties(target, style.StyleRunProperties);
				}
			}
		}

		private void ApplyRunStyleRecursive(ResolvedRunStyle target, string styleId) {
			if (_stylesById.TryGetValue(styleId, out Style? style)) {
				if (style.BasedOn?.Val != null) {
					ApplyRunStyleRecursive(target, style.BasedOn.Val.Value!);
				}
				if (style.StyleRunProperties != null) {
					ApplyRunProperties(target, style.StyleRunProperties);
				}
			}
		}

		private void ApplyParagraphProperties(ResolvedParagraphStyle target, OpenXmlCompositeElement pPr) {
			// Alignment (w:jc)
			Justification? jc = pPr.GetFirstChild<Justification>();
			if (jc?.Val != null) {
				target.Alignment = MapJustification(jc.Val.Value);
			}

			// Spacing (w:spacing)
			SpacingBetweenLines? spacing = pPr.GetFirstChild<SpacingBetweenLines>();
			if (spacing != null) {
				if (spacing.Before != null && double.TryParse(spacing.Before.Value, out double beforeTwips)) {
					target.SpacingBeforePt = TwipConverter.TwipsToPoints(beforeTwips);
				}
				if (spacing.After != null && double.TryParse(spacing.After.Value, out double afterTwips)) {
					target.SpacingAfterPt = TwipConverter.TwipsToPoints(afterTwips);
				}
				if (spacing.Line != null && double.TryParse(spacing.Line.Value, out double lineVal)) {
					if (spacing.LineRule?.Value == LineSpacingRuleValues.Exact || spacing.LineRule?.Value == LineSpacingRuleValues.AtLeast) {
						target.LineHeightPt = TwipConverter.TwipsToPoints(lineVal);
						target.IsLineHeightMultiple = false;
					} else {
						// 240 units = 1.0 (multiple line spacing)
						target.LineHeightPt = lineVal / 240.0;
						target.IsLineHeightMultiple = true;
					}
				}
			}

			// Indentation (w:ind)
			Indentation? ind = pPr.GetFirstChild<Indentation>();
			if (ind != null) {
				if (ind.Left != null && double.TryParse(ind.Left.Value, out double leftTwips)) {
					target.LeftIndentPt = TwipConverter.TwipsToPoints(leftTwips);
				}
				if (ind.Right != null && double.TryParse(ind.Right.Value, out double rightTwips)) {
					target.RightIndentPt = TwipConverter.TwipsToPoints(rightTwips);
				}
				if (ind.FirstLine != null && double.TryParse(ind.FirstLine.Value, out double firstLineTwips)) {
					target.FirstLineIndentPt = TwipConverter.TwipsToPoints(firstLineTwips);
				}
				if (ind.Hanging != null && double.TryParse(ind.Hanging.Value, out double hangingTwips)) {
					target.HangingIndentPt = TwipConverter.TwipsToPoints(hangingTwips);
				}
			}
		}

		private void ApplyRunProperties(ResolvedRunStyle target, OpenXmlCompositeElement rPr) {
			// Font Family (w:rFonts)
			RunFonts? fonts = rPr.GetFirstChild<RunFonts>();
			if (fonts != null) {
				string? fontName = fonts.Ascii?.Value ?? fonts.HighAnsi?.Value ?? fonts.ComplexScript?.Value;
				if (!string.IsNullOrEmpty(fontName)) {
					target.FontFamily = fontName!;
				}
			}

			// Font Size (w:sz) - stored in half-points
			FontSize? sz = rPr.GetFirstChild<FontSize>();
			if (sz?.Val?.Value != null && double.TryParse(sz.Val.Value, out double halfPts)) {
				target.FontSizePt = TwipConverter.HalfPointsToPoints(halfPts);
			}

			// Bold (w:b)
			Bold? bold = rPr.GetFirstChild<Bold>();
			if (bold != null) {
				target.IsBold = bold.Val?.Value ?? true;
			}

			// Italic (w:i)
			Italic? italic = rPr.GetFirstChild<Italic>();
			if (italic != null) {
				target.IsItalic = italic.Val?.Value ?? true;
			}

			// Underline (w:u)
			Underline? underline = rPr.GetFirstChild<Underline>();
			if (underline != null) {
				target.IsUnderline = underline.Val?.Value != UnderlineValues.None;
			}

			// StrikeThrough (w:strike)
			Strike? strike = rPr.GetFirstChild<Strike>();
			if (strike != null) {
				target.IsStrikeThrough = strike.Val?.Value ?? true;
			}

			// Text Color (w:color)
			Color? color = rPr.GetFirstChild<Color>();
			if (color != null) {
				if (color.Val?.Value != null) {
					target.TextColorHex = TwipConverter.NormalizeHexColor(color.Val.Value, "#000000");
				} else if (color.ThemeColor?.Value != null) {
					target.TextColorHex = "#000000";
				}
			}

			// Background Shading (w:shd) or Highlight (w:highlight)
			Shading? shading = rPr.GetFirstChild<Shading>();
			if (shading?.Fill?.Value != null) {
				string bg = TwipConverter.NormalizeHexColor(shading.Fill.Value, string.Empty);
				if (!string.IsNullOrEmpty(bg)) {
					target.BackgroundColorHex = bg;
				}
			}
		}

		private ParagraphAlignment MapJustification(JustificationValues val) {
			if (val == JustificationValues.Center) return ParagraphAlignment.Center;
			if (val == JustificationValues.Right) return ParagraphAlignment.Right;
			if (val == JustificationValues.Both) return ParagraphAlignment.Justify;
			return ParagraphAlignment.Left;
		}
	}
}
