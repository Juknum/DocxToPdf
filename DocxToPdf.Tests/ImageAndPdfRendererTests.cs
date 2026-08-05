using Xunit;
using PdfSharp.Pdf;
using DocxToPdf.Model;
using DocxToPdf.Rendering;
using DocxToPdf.Fonts;

namespace DocxToPdf.Tests {
	public class ImageAndPdfRendererTests {

		static ImageAndPdfRendererTests() {
			CrossPlatformFontResolver.Register();
		}

		[Fact]
		public void TestMeasureDrawingScaling() {
			DrawingModel drawing = new DrawingModel {
				WidthPt = 500,
				HeightPt = 300,
				Placement = DrawingPlacement.Inline
			};

			var (w, h) = ImageRenderer.MeasureDrawing(drawing, containerWidth: 250);

			Assert.Equal(250, w);
			Assert.Equal(150, h);
		}

		[Fact]
		public void TestPdfRendererEndToEndDocument() {
			DocumentModel doc = new DocumentModel();
			SectionModel section = new SectionModel {
				PageSetup = new PageSetupModel {
					Width = 612,
					Height = 792,
					Margins = new PageMarginsModel { Top = 50, Bottom = 50, Left = 50, Right = 50 }
				}
			};

			section.HeaderDefault = new HeaderFooterModel {
				Elements = {
					new ParagraphModel {
						Runs = { new RunModel { Text = "Document Header - Page ", FontSizePt = 9 }, new RunModel { Field = FieldType.PageNumber, FontSizePt = 9 } }
					}
				}
			};

			section.FooterDefault = new HeaderFooterModel {
				Elements = {
					new ParagraphModel {
						Runs = { new RunModel { Text = "Page ", FontSizePt = 9 }, new RunModel { Field = FieldType.PageNumber, FontSizePt = 9 }, new RunModel { Text = " of ", FontSizePt = 9 }, new RunModel { Field = FieldType.TotalPages, FontSizePt = 9 } }
					}
				}
			};

			// Add multiple paragraphs to trigger page break
			for (int i = 1; i <= 40; i++) {
				section.Elements.Add(new ParagraphModel {
					SpacingBeforePt = 5,
					SpacingAfterPt = 5,
					Runs = {
						new RunModel { Text = $"Paragraph #{i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.", FontSizePt = 11 }
					}
				});
			}

			doc.Sections.Add(section);

			using PdfDocument pdf = PdfRenderer.Render(doc);

			Assert.NotNull(pdf);
			Assert.True(pdf.PageCount >= 2, $"Expected multi-page PDF output due to 40 paragraphs, got {pdf.PageCount} page(s)");
		}
	}
}
