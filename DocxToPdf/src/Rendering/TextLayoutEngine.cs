using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	public class MeasuredRunFragment {
		public RunModel Run { get; set; } = new RunModel();
		public string Text { get; set; } = string.Empty;
		public XFont Font { get; set; } = null!;
		public XColor Color { get; set; }
		public XColor? BackgroundColor { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
	}

	public class TextLineLayout {
		public List<MeasuredRunFragment> Fragments { get; set; } = new List<MeasuredRunFragment>();
		public double LineWidth => Fragments.Sum(f => f.Width);
		public double LineHeight { get; set; }
		public double IndentX { get; set; }
		public double AvailableWidth { get; set; }
		public ParagraphAlignment Alignment { get; set; }
		public bool IsLastLineOfParagraph { get; set; }
	}

	public class ParagraphLayout {
		public ParagraphModel Paragraph { get; set; } = new ParagraphModel();
		public List<TextLineLayout> Lines { get; set; } = new List<TextLineLayout>();
		public double SpacingBefore { get; set; }
		public double SpacingAfter { get; set; }
		public double TotalHeight => SpacingBefore + Lines.Sum(l => l.LineHeight) + SpacingAfter;

		public string? MarkerText { get; set; }
		public XFont? MarkerFont { get; set; }
		public double MarkerX { get; set; }
	}

	public static class TextLayoutEngine {

		public static ParagraphLayout MeasureParagraph(ParagraphModel paragraph, XGraphics gfx, double containerWidth, int currentPage = 1, int totalPages = 1) {
			ParagraphLayout layout = new ParagraphLayout {
				Paragraph = paragraph,
				SpacingBefore = paragraph.SpacingBeforePt,
				SpacingAfter = paragraph.SpacingAfterPt
			};

			double leftIndent = paragraph.LeftIndentPt;
			double rightIndent = paragraph.RightIndentPt;
			double firstLineIndent = paragraph.FirstLineIndentPt;
			double hangingIndent = paragraph.HangingIndentPt;

			// Handle List formatting
			if (paragraph.ListFormat != null) {
				var listFmt = paragraph.ListFormat;
				if (listFmt.LeftIndentPt > 0) leftIndent = listFmt.LeftIndentPt;
				if (listFmt.HangingIndentPt > 0) hangingIndent = listFmt.HangingIndentPt;

				layout.MarkerText = listFmt.MarkerText;
				// Default marker font to first run's font or Arial 11pt
				RunModel firstRun = paragraph.Runs.FirstOrDefault() ?? new RunModel();
				layout.MarkerFont = TextMeasurer.CreateFont(firstRun);

				// Position marker at leftIndent - hangingIndent
				layout.MarkerX = Math.Max(0, leftIndent - (hangingIndent > 0 ? hangingIndent : 18.0));
			}

			// If paragraph has no runs, treat as empty line paragraph (e.g. blank paragraph spacing)
			if (paragraph.Runs.Count == 0) {
				XFont defaultFont = TextMeasurer.CreateFont("Arial", 11.0);
				double emptyLineHeight = CalculateLineHeight(paragraph, defaultFont.Height);
				layout.Lines.Add(new TextLineLayout {
					LineHeight = emptyLineHeight,
					IndentX = leftIndent + firstLineIndent,
					AvailableWidth = Math.Max(1.0, containerWidth - leftIndent - rightIndent - firstLineIndent),
					Alignment = paragraph.Alignment,
					IsLastLineOfParagraph = true
				});
				return layout;
			}

			// Flatten runs into measured word tokens
			List<MeasuredRunFragment> tokens = TokenizeParagraph(paragraph, gfx, currentPage, totalPages);
			if (tokens.Count == 0) {
				XFont defaultFont = TextMeasurer.CreateFont(paragraph.Runs[0]);
				double emptyLineHeight = CalculateLineHeight(paragraph, defaultFont.Height);
				layout.Lines.Add(new TextLineLayout {
					LineHeight = emptyLineHeight,
					IndentX = leftIndent + firstLineIndent,
					AvailableWidth = Math.Max(1.0, containerWidth - leftIndent - rightIndent - firstLineIndent),
					Alignment = paragraph.Alignment,
					IsLastLineOfParagraph = true
				});
				return layout;
			}

			// Wrap tokens into lines
			int lineIndex = 0;
			int tokenIndex = 0;

			while (tokenIndex < tokens.Count) {
				double curLineIndent = (lineIndex == 0) ? (leftIndent + firstLineIndent) : (leftIndent + hangingIndent);
				double availWidth = Math.Max(10.0, containerWidth - curLineIndent - rightIndent);

				TextLineLayout line = new TextLineLayout {
					IndentX = curLineIndent,
					AvailableWidth = availWidth,
					Alignment = paragraph.Alignment
				};

				double currentLineWidth = 0;
				double maxFontHeight = 0;

				while (tokenIndex < tokens.Count) {
					MeasuredRunFragment token = tokens[tokenIndex];

					// Check explicit newline token
					if (token.Text == "\n") {
						tokenIndex++;
						break;
					}

					if (line.Fragments.Count > 0 && (currentLineWidth + token.Width > availWidth)) {
						// Line full, break to next line
						break;
					}

					line.Fragments.Add(token);
					currentLineWidth += token.Width;
					if (token.Height > maxFontHeight) {
						maxFontHeight = token.Height;
					}
					tokenIndex++;
				}

				if (maxFontHeight == 0 && line.Fragments.Count > 0) {
					maxFontHeight = line.Fragments.Max(f => f.Height);
				}

				line.LineHeight = CalculateLineHeight(paragraph, maxFontHeight > 0 ? maxFontHeight : 12.0);
				layout.Lines.Add(line);
				lineIndex++;
			}

			if (layout.Lines.Count > 0) {
				layout.Lines.Last().IsLastLineOfParagraph = true;
			}

			return layout;
		}

		private static List<MeasuredRunFragment> TokenizeParagraph(ParagraphModel paragraph, XGraphics gfx, int currentPage, int totalPages) {
			List<MeasuredRunFragment> result = new List<MeasuredRunFragment>();

			foreach (var run in paragraph.Runs) {
				string text = run.Text;
				if (run.Field == FieldType.PageNumber) {
					text = currentPage.ToString();
				} else if (run.Field == FieldType.TotalPages) {
					text = totalPages.ToString();
				}

				if (string.IsNullOrEmpty(text)) continue;

				XFont font = TextMeasurer.CreateFont(run);
				XColor color = TextMeasurer.ParseColor(run.TextColorHex, XColors.Black);
				XColor? bgColor = !string.IsNullOrEmpty(run.BackgroundColorHex) ? TextMeasurer.ParseColor(run.BackgroundColorHex, XColors.Transparent) : (XColor?)null;

				// Handle line breaks within text
				string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
				for (int i = 0; i < lines.Length; i++) {
					if (i > 0) {
						result.Add(new MeasuredRunFragment { Text = "\n" });
					}

					string lineText = lines[i];
					if (string.IsNullOrEmpty(lineText)) continue;

					// Split into words while keeping trailing spaces
					string[] words = SplitIntoWords(lineText);
					foreach (var word in words) {
						XSize sz = TextMeasurer.MeasureString(gfx, word, font);
						result.Add(new MeasuredRunFragment {
							Run = run,
							Text = word,
							Font = font,
							Color = color,
							BackgroundColor = bgColor,
							Width = sz.Width,
							Height = font.Height
						});
					}
				}
			}

			return result;
		}

		private static string[] SplitIntoWords(string text) {
			List<string> words = new List<string>();
			int start = 0;
			for (int i = 0; i < text.Length; i++) {
				if (text[i] == ' ') {
					words.Add(text.Substring(start, i - start + 1));
					start = i + 1;
				}
			}
			if (start < text.Length) {
				words.Add(text.Substring(start));
			}
			return words.ToArray();
		}

		private static double CalculateLineHeight(ParagraphModel paragraph, double baseFontHeight) {
			if (paragraph.LineHeightPt > 0) {
				if (paragraph.IsLineHeightMultiple) {
					return baseFontHeight * paragraph.LineHeightPt;
				}
				return paragraph.LineHeightPt;
			}
			return baseFontHeight * 1.15; // Standard Word line spacing ~1.15x
		}

		public static void RenderParagraph(ParagraphLayout layout, XGraphics gfx, double containerX, ref double currentY) {
			currentY += layout.SpacingBefore;

			// Render List Marker if present
			if (!string.IsNullOrEmpty(layout.MarkerText) && layout.MarkerFont != null) {
				XColor markerColor = XColors.Black;
				XSolidBrush markerBrush = new XSolidBrush(markerColor);
				double markerY = currentY;
				if (layout.Lines.Count > 0) {
					markerY = currentY + (layout.Lines[0].LineHeight - layout.MarkerFont.Height) / 2.0;
				}
				gfx.DrawString(layout.MarkerText!, layout.MarkerFont, markerBrush, containerX + layout.MarkerX, markerY + layout.MarkerFont.Height * 0.8);
			}

			foreach (var line in layout.Lines) {
				double startX = containerX + line.IndentX;

				if (line.Alignment == ParagraphAlignment.Center) {
					double extraSpace = line.AvailableWidth - line.LineWidth;
					if (extraSpace > 0) startX += extraSpace / 2.0;
				} else if (line.Alignment == ParagraphAlignment.Right) {
					double extraSpace = line.AvailableWidth - line.LineWidth;
					if (extraSpace > 0) startX += extraSpace;
				}

				double justifySpacing = 0;
				if (line.Alignment == ParagraphAlignment.Justify && !line.IsLastLineOfParagraph && line.Fragments.Count > 1) {
					double extraSpace = line.AvailableWidth - line.LineWidth;
					if (extraSpace > 0) {
						justifySpacing = extraSpace / (line.Fragments.Count - 1);
					}
				}

				double curX = startX;

				foreach (var fragment in line.Fragments) {
					if (string.IsNullOrEmpty(fragment.Text)) continue;

					double drawY = currentY + (line.LineHeight - fragment.Height) / 2.0 + fragment.Height * 0.8;

					// Background shading
					if (fragment.BackgroundColor.HasValue && fragment.BackgroundColor.Value != XColors.Transparent) {
						XSolidBrush bgBrush = new XSolidBrush(fragment.BackgroundColor.Value);
						gfx.DrawRectangle(bgBrush, curX, currentY + (line.LineHeight - fragment.Height) / 2.0, fragment.Width + justifySpacing, fragment.Height);
					}

					// Text
					XSolidBrush textBrush = new XSolidBrush(fragment.Color);
					gfx.DrawString(fragment.Text, fragment.Font, textBrush, curX, drawY);

					curX += fragment.Width + justifySpacing;
				}

				currentY += line.LineHeight;
			}

			currentY += layout.SpacingAfter;
		}
	}
}
