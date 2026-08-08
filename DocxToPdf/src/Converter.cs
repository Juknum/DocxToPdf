using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocxToPdf.Fonts;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using DocxToPdf.Rendering;
using PdfSharp.Pdf;

namespace DocxToPdf {
	/// <summary>
	/// Provides primary entry point APIs and service implementation for parsing DOCX documents into internal document models and converting them into PDF files.
	/// </summary>
	public class Converter : IConverter {

		static Converter() {
			// Register cross-platform font resolver for PDFsharp
			CrossPlatformFontResolver.Register();
		}

		/// <inheritdoc />
		public DocumentModel ParseDocument(string wordFilePath) => Parse(wordFilePath);

		/// <inheritdoc />
		public void ConvertDocument(string wordFilePath, string outputPdfFilePath) => Convert(wordFilePath, outputPdfFilePath);

		/// <inheritdoc />
		public static DocumentModel Parse(string wordFilePath) {
			if (wordFilePath == null) throw new ArgumentNullException(nameof(wordFilePath));

			if (!File.Exists(wordFilePath)) {
				throw new FileNotFoundException($"Input document not found: '{wordFilePath}'");
			}

			using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, false);
			return DocxParser.Parse(wordDoc);
		}

		/// <inheritdoc />
		public static void Convert(string wordFilePath, string outputPdfFilePath) {
			if (wordFilePath == null) throw new ArgumentNullException(nameof(wordFilePath));
			if (outputPdfFilePath == null) throw new ArgumentNullException(nameof(outputPdfFilePath));

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
