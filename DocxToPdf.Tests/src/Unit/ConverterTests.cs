using System.IO;
using DocxToPdf.Model;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class ConverterTests {
		[Fact]
		public void TestParseFileNotFoundThrows() {
			Assert.Throws<FileNotFoundException>(() => Converter.Parse("non_existent_file.docx"));
		}

		[Fact]
		public void TestConvertFileNotFoundThrows() {
			Assert.Throws<FileNotFoundException>(() => Converter.Convert("non_existent_file.docx", "output.pdf"));
		}

		[Fact]
		public void TestParseAndConvertValidFile() {
			string tempDocxPath = Path.Combine(Path.GetTempPath(), $"test_{System.Guid.NewGuid()}.docx");
			string tempPdfDir = Path.Combine(Path.GetTempPath(), $"pdf_dir_{System.Guid.NewGuid()}");
			string tempPdfPath = Path.Combine(tempPdfDir, "output.pdf");

			try {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(tempDocxPath, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					mainPart.Document = new Document(new Body(
						new Paragraph(new Run(new Text("Test Converter")))
					));
					wordDoc.Save();
				}

				DocumentModel model = Converter.Parse(tempDocxPath);
				Assert.NotNull(model);
				Assert.Single(model.Sections);

				Converter.Convert(tempDocxPath, tempPdfPath);
				Assert.True(File.Exists(tempPdfPath));
				Assert.True(new FileInfo(tempPdfPath).Length > 0);
			} finally {
				if (File.Exists(tempDocxPath)) File.Delete(tempDocxPath);
				if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
				if (Directory.Exists(tempPdfDir)) Directory.Delete(tempPdfDir, true);
			}
		}

		[Fact]
		public void TestConvertComplexDocumentWithTablesAndLists() {
			string tempDocxPath = Path.Combine(Path.GetTempPath(), $"test_complex_{System.Guid.NewGuid()}.docx");
			string tempPdfPath = Path.Combine(Path.GetTempPath(), $"test_complex_{System.Guid.NewGuid()}.pdf");

			try {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(tempDocxPath, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					Table table = new Table(
						new TableProperties(
							new TableBorders(
								new TopBorder { Val = BorderValues.Single, Size = 12, Color = "000000" },
								new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "000000" }
							)
						),
						new TableRow(
							new TableCell(
								new TableCellProperties(new Shading { Fill = "CCCCCC" }),
								new Paragraph(new Run(new Text("Header 1")))
							),
							new TableCell(
								new TableCellProperties(new Shading { Fill = "CCCCCC" }),
								new Paragraph(new Run(new Text("Header 2")))
							)
						),
						new TableRow(
							new TableCell(new Paragraph(new Run(new Text("Cell 1")))),
							new TableCell(new Paragraph(new Run(new Text("Cell 2"))))
						)
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(new Run(new RunProperties(new Bold(), new FontSize { Val = "36" }), new Text("Complex Document Title"))),
						new Paragraph(new Run(new Text("Paragraph with sample body text."))),
						table
					));
					wordDoc.Save();
				}

				Converter.Convert(tempDocxPath, tempPdfPath);
				Assert.True(File.Exists(tempPdfPath));
				Assert.True(new FileInfo(tempPdfPath).Length > 0);
			} finally {
				if (File.Exists(tempDocxPath)) File.Delete(tempDocxPath);
				if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
			}
		}

		[Fact]
		public void TestConvertMultiPageDocument() {
			string tempDocxPath = Path.Combine(Path.GetTempPath(), $"test_multipage_{System.Guid.NewGuid()}.docx");
			string tempPdfPath = Path.Combine(Path.GetTempPath(), $"test_multipage_{System.Guid.NewGuid()}.pdf");

			try {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(tempDocxPath, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					Body body = new Body();

					// Add enough paragraphs to trigger page overflow
					for (int i = 1; i <= 50; i++) {
						body.Append(new Paragraph(new Run(new Text($"Multi-page test paragraph line number {i} with additional content text to wrap appropriately."))));
					}

					mainPart.Document = new Document(body);
					wordDoc.Save();
				}

				Converter.Convert(tempDocxPath, tempPdfPath);
				Assert.True(File.Exists(tempPdfPath));
			} finally {
				if (File.Exists(tempDocxPath)) File.Delete(tempDocxPath);
				if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
			}
		}

		[Fact]
		public void TestEmfRasterizerInvalidDataReturnsFalse() {
			using var pdf = new PdfSharp.Pdf.PdfDocument();
			var page = pdf.AddPage();
			using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

			bool resultNull = DocxToPdf.Rendering.EmfRasterizer.RenderEmf(null!, gfx, 0, 0, 100, 100);
			Assert.False(resultNull);

			bool resultEmpty = DocxToPdf.Rendering.EmfRasterizer.RenderEmf(new byte[10], gfx, 0, 0, 100, 100);
			Assert.False(resultEmpty);
		}

		[Fact]
		public void TestEmfRasterizerValidEmfStream() {
			string filesDir = E2EWorkflowTests.FindDocxToPdfFilesDirectory();
			string inputDocx = Path.Combine(filesDir, "InternshipCover", "input.docx");
			using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(inputDocx, false);
			bool foundEmf = false;
			foreach (var part in wordDoc.MainDocumentPart!.Parts) {
				if (part.OpenXmlPart.Uri.ToString().EndsWith(".emf", System.StringComparison.OrdinalIgnoreCase)) {
					foundEmf = true;
					using var stream = part.OpenXmlPart.GetStream();
					using var ms = new MemoryStream();
					stream.CopyTo(ms);
					byte[] bytes = ms.ToArray();

					using var pdf = new PdfSharp.Pdf.PdfDocument();
					var page = pdf.AddPage();
					using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

					bool rendered = DocxToPdf.Rendering.EmfRasterizer.RenderEmf(bytes, gfx, 0, 0, 500, 30);
					Assert.True(rendered);
				}
			}
			foreach (var imagePart in wordDoc.MainDocumentPart.ImageParts) {
				if (imagePart.Uri.ToString().EndsWith(".emf", System.StringComparison.OrdinalIgnoreCase)) {
					foundEmf = true;
					using var stream = imagePart.GetStream();
					using var ms = new MemoryStream();
					stream.CopyTo(ms);
					byte[] bytes = ms.ToArray();

					using var pdf = new PdfSharp.Pdf.PdfDocument();
					var page = pdf.AddPage();
					using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

					bool rendered = DocxToPdf.Rendering.EmfRasterizer.RenderEmf(bytes, gfx, 0, 0, 500, 30);
					Assert.True(rendered);
				}
			}
			Assert.True(foundEmf);
		}

		[Fact]
		public void TestCalculateXPositioning() {
			var model = new DocxToPdf.Model.DrawingModel {
				Placement = DocxToPdf.Model.DrawingPlacement.Floating,
				HorizontalRelativeFrom = "margin",
				OffsetXPt = 15.0
			};
			double xMargin = DocxToPdf.Rendering.ImageRenderer.CalculateX(model, 72.0, 468.0, 100.0);
			Assert.Equal(87.0, xMargin);

			model.AlignH = "center";
			double xCenter = DocxToPdf.Rendering.ImageRenderer.CalculateX(model, 72.0, 468.0, 100.0);
			Assert.Equal(256.0, xCenter); // 72 + (468 - 100) / 2
		}

		[Fact]
		public void TestCalculateYPositioning() {
			var model = new DocxToPdf.Model.DrawingModel {
				Placement = DocxToPdf.Model.DrawingPlacement.Floating,
				VerticalRelativeFrom = "topmargin",
				OffsetYPt = 25.0
			};
			double yMargin = DocxToPdf.Rendering.ImageRenderer.CalculateY(model, 150.0, 50.0);
			Assert.Equal(97.0, yMargin); // 72 + 25

			model.VerticalRelativeFrom = "paragraph";
			double yParagraph = DocxToPdf.Rendering.ImageRenderer.CalculateY(model, 150.0, 50.0);
			Assert.Equal(175.0, yParagraph); // 150 + 25
		}
	}
}

