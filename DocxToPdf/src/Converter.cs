using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocxToPdf.Fonts;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using DocxToPdf.Rendering;
using PdfSharp.Pdf;

namespace DocxToPdf {
	public class Converter {

		static Converter() {
			// Register cross-platform font resolver for PDFsharp
			CrossPlatformFontResolver.Register();
		}

		public static DocumentModel Parse(string wordFilePath) {
			if (!File.Exists(wordFilePath)) {
				throw new FileNotFoundException($"Input document not found: '{wordFilePath}'");
			}

			using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, false);
			return DocxParser.Parse(wordDoc);
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
			DocumentModel docModel = DocxParser.Parse(wordDoc);

			using PdfDocument pdfDoc = PdfRenderer.Render(docModel);
			int pageCount = pdfDoc.PageCount;
			pdfDoc.Save(outputPdfFilePath);

			Console.WriteLine($"Successfully parsed and converted document ({docModel.Sections.Count} section(s), {pageCount} page(s)) to '{outputPdfFilePath}'.");
		}
	}
}
