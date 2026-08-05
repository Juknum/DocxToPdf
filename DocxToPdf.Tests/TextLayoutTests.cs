using System.Linq;
using Xunit;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using DocxToPdf.Model;
using DocxToPdf.Rendering;
using DocxToPdf.Fonts;

namespace DocxToPdf.Tests {
	public class TextLayoutTests {

		static TextLayoutTests() {
			CrossPlatformFontResolver.Register();
		}

		[Fact]
		public void TestTextMeasurerColorParsing() {
			XColor red = TextMeasurer.ParseColor("#FF0000", XColors.Black);
			Assert.Equal(255, red.R);
			Assert.Equal(0, red.G);
			Assert.Equal(0, red.B);

			XColor fallback = TextMeasurer.ParseColor("invalid", XColors.Blue);
			Assert.Equal(XColors.Blue, fallback);
		}

		[Fact]
		public void TestMeasureParagraphWrapping() {
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			ParagraphModel p = new ParagraphModel {
				Alignment = ParagraphAlignment.Left,
				SpacingBeforePt = 10,
				SpacingAfterPt = 12
			};

			p.Runs.Add(new RunModel {
				Text = "This is a very long line of text intended to test the automatic line wrapping functionality of the TextLayoutEngine component in PDFsharp rendering.",
				FontFamily = "Arial",
				FontSizePt = 12
			});

			ParagraphLayout layout = TextLayoutEngine.MeasureParagraph(p, gfx, containerWidth: 200);

			Assert.True(layout.Lines.Count > 1, "Long text should wrap into multiple lines when container width is narrow");
			Assert.Equal(10, layout.SpacingBefore);
			Assert.Equal(12, layout.SpacingAfter);
			Assert.True(layout.TotalHeight > 0);
		}

		[Fact]
		public void TestFieldSubstitution() {
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			ParagraphModel p = new ParagraphModel();
			p.Runs.Add(new RunModel { Text = "Page ", FontFamily = "Arial", FontSizePt = 10 });
			p.Runs.Add(new RunModel { Field = FieldType.PageNumber, FontFamily = "Arial", FontSizePt = 10 });
			p.Runs.Add(new RunModel { Text = " of ", FontFamily = "Arial", FontSizePt = 10 });
			p.Runs.Add(new RunModel { Field = FieldType.TotalPages, FontFamily = "Arial", FontSizePt = 10 });

			ParagraphLayout layout = TextLayoutEngine.MeasureParagraph(p, gfx, containerWidth: 500, currentPage: 3, totalPages: 10);

			Assert.Single(layout.Lines);
			string fullText = string.Join("", layout.Lines[0].Fragments.Select(f => f.Text));
			Assert.Equal("Page 3 of 10", fullText);
		}

		[Fact]
		public void TestListFormattingMarkerLayout() {
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			ParagraphModel p = new ParagraphModel {
				ListFormat = new ListFormatModel {
					MarkerText = "•",
					LeftIndentPt = 36,
					HangingIndentPt = 18
				}
			};
			p.Runs.Add(new RunModel { Text = "Bullet item 1", FontFamily = "Arial", FontSizePt = 11 });

			ParagraphLayout layout = TextLayoutEngine.MeasureParagraph(p, gfx, containerWidth: 400);

			Assert.Equal("•", layout.MarkerText);
			Assert.Equal(18, layout.MarkerX);
		}
	}
}
