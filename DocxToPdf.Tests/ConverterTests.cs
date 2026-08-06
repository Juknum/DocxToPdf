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
	}
}

