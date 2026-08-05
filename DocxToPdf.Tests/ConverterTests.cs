using System.IO;
using DocxToPdf.Model;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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
			} finally {
				if (File.Exists(tempDocxPath)) File.Delete(tempDocxPath);
				if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
				if (Directory.Exists(tempPdfDir)) Directory.Delete(tempPdfDir, true);
			}
		}
	}
}
