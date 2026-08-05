using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocxToPdf.Fonts;
using PdfSharp.Pdf;

namespace DocxToPdf {
	public class Converter {

		static Converter() {
			// Register cross-platform font resolver for PDFsharp
			CrossPlatformFontResolver.Register();
		}

		public static void Convert(string wordFilePath, string outputPdfFilePath) {
			if (!File.Exists(wordFilePath)) {
				throw new FileNotFoundException($"Input document not found: '{wordFilePath}'");
			}

			Console.WriteLine($"Converting '{wordFilePath}' to PDF using OpenXML + PDFsharp...");

			// Ensure target output directory exists
			string? outputDir = Path.GetDirectoryName(outputPdfFilePath);
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) {
				Directory.CreateDirectory(outputDir);
			}

			using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, false);
			using PdfDocument pdfDoc = new PdfDocument();

			// Initial stub PDF page generation until layout engine (Phase 2 & 3) is implemented
			PdfPage page = pdfDoc.AddPage();
			pdfDoc.Save(outputPdfFilePath);

			Console.WriteLine($"Generated initial PDF output at '{outputPdfFilePath}'.");
		}
	}
}
