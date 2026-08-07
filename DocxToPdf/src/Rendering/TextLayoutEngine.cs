using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Represents a measured slice/word fragment of a text run with font, color, and bounding dimensions.
	/// </summary>
	public class MeasuredRunFragment {
		/// <summary>Gets or sets the parent run model.</summary>
		public RunModel Run { get; set; } = new();
		/// <summary>Gets or sets fragment text string.</summary>
		public string Text { get; set; } = string.Empty;
		/// <summary>Gets or sets PDFsharp font instance.</summary>
		public XFont Font { get; set; } = null!;
		/// <summary>Gets or sets text foreground color.</summary>
		public XColor Color { get; set; }
		/// <summary>Gets or sets text background shading color.</summary>
		public XColor? BackgroundColor { get; set; }
		/// <summary>Gets or sets fragment width in points.</summary>
		public double Width { get; set; }
		/// <summary>Gets or sets fragment height in points.</summary>
		public double Height { get; set; }
	}

	/// <summary>
	/// Represents a single wrapped line layout containing fragments, line height, alignment, and indentation.
	/// </summary>
	public class TextLineLayout {
		/// <summary>Gets or sets measured fragments in this line.</summary>
		public List<MeasuredRunFragment> Fragments { get; set; } = [];
		/// <summary>Gets total line width in points.</summary>
		public double LineWidth => Fragments.Sum(f => f.Width);
		/// <summary>Gets or sets maximum line height in points.</summary>
		public double LineHeight { get; set; }
		/// <summary>Gets or sets horizontal X indentation offset for this line.</summary>
		public double IndentX { get; set; }
		/// <summary>Gets or sets available printable width for line wrapping.</summary>
		public double AvailableWidth { get; set; }
		/// <summary>Gets or sets line text alignment.</summary>
		public ParagraphAlignment Alignment { get; set; }
		/// <summary>Gets or sets whether this line is the final line of a paragraph.</summary>
		public bool IsLastLineOfParagraph { get; set; }
	}

	/// <summary>
	/// Represents complete layout metrics for a paragraph including wrapped lines, list markers, and vertical spacing.
	/// </summary>
	public class ParagraphLayout {
		/// <summary>Gets or sets the underlying paragraph model.</summary>
		public ParagraphModel Paragraph { get; set; } = new();
		/// <summary>Gets or sets measured line layouts.</summary>
		public List<TextLineLayout> Lines { get; set; } = [];
		/// <summary>Gets or sets top vertical spacing in points.</summary>
		public double SpacingBefore { get; set; }
		/// <summary>Gets or sets bottom vertical spacing in points.</summary>
		public double SpacingAfter { get; set; }
		/// <summary>Gets total paragraph height including spacing and line heights.</summary>
		public double TotalHeight => SpacingBefore + Lines.Sum(l => l.LineHeight) + SpacingAfter;

		/// <summary>Gets or sets bullet or list marker text string.</summary>
		public string? MarkerText { get; set; }
		/// <summary>Gets or sets bullet or list marker font.</summary>
		public XFont? MarkerFont { get; set; }
		/// <summary>Gets or sets bullet or list marker X origin.</summary>
		public double MarkerX { get; set; }
	}

	/// <summary>
	/// Text layout engine responsible for word wrapping, paragraph measurement, justification, and rendering onto XGraphics.
	/// </summary>
	public static class TextLayoutEngine {

		/// <summary>
		/// Measures and word-wraps a paragraph model into a <see cref="ParagraphLayout"/> within printable container constraints.
		/// </summary>
		/// <param name="paragraph">The paragraph model. Cannot be null.</param>
		/// <param name="gfx">PDFsharp graphics context. Cannot be null.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		/// <param name="previousSpacingAfter">Spacing after from the previous paragraph for collapsing rules.</param>
		/// <param name="currentPage">Current 1-indexed page number.</param>
		/// <param name="totalPages">Total page count.</param>
		/// <returns>A populated <see cref="ParagraphLayout"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="paragraph"/> or <paramref name="gfx"/> is null.</exception>
		public static ParagraphLayout MeasureParagraph(ParagraphModel paragraph, XGraphics gfx, double containerWidth, double previousSpacingAfter = 0, int currentPage = 1, int totalPages = 1) {
			if (paragraph == null) throw new ArgumentNullException(nameof(paragraph));
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));

			ParagraphLayout layout = new() {
				Paragraph = paragraph,
				SpacingBefore = Math.Max(0, paragraph.SpacingBeforePt - previousSpacingAfter),
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
			return baseFontHeight; // baseFontHeight (XFont.Height) already includes default line leading
		}

		/// <summary>
		/// Renders a measured paragraph layout onto the PDF graphics canvas at the specified coordinates.
		/// </summary>
		/// <param name="layout">The measured paragraph layout model. Cannot be null.</param>
		/// <param name="gfx">PDFsharp graphics context. Cannot be null.</param>
		/// <param name="containerX">Left margin origin in points.</param>
		/// <param name="currentY">Ref top Y coordinate in points updated as lines are drawn.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="layout"/> or <paramref name="gfx"/> is null.</exception>
		public static void RenderParagraph(ParagraphLayout layout, XGraphics gfx, double containerX, ref double currentY) {
			if (layout == null) throw new ArgumentNullException(nameof(layout));
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));

			currentY += layout.SpacingBefore;

			// Render Paragraph Background Shading if present
			if (!string.IsNullOrEmpty(layout.Paragraph.BackgroundColorHex) && !string.Equals(layout.Paragraph.BackgroundColorHex, "auto", StringComparison.OrdinalIgnoreCase)) {
				XColor bgCol = TextMeasurer.ParseColor(layout.Paragraph.BackgroundColorHex, XColors.Transparent);
				if (bgCol != XColors.Transparent) {
					double pHeight = layout.Lines.Sum(l => l.LineHeight);
					if (pHeight > 0) {
						gfx.DrawRectangle(new XSolidBrush(bgCol), containerX, currentY, Math.Max(10.0, gfx.PageSize.Width - containerX * 2.0), pHeight);
					}
				}
			}

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
					if (fragment.BackgroundColor.HasValue && fragment.BackgroundColor.Value != XColors.Transparent && fragment.BackgroundColor.Value != XColors.White) {
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
