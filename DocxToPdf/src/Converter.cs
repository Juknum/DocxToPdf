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
	/// Provides primary entry point APIs for parsing DOCX documents into internal document models and converting them into PDF files.
	/// </summary>
	public class Converter {

		static Converter() {
			// Register cross-platform font resolver for PDFsharp
			CrossPlatformFontResolver.Register();
		}

		/// <summary>
		/// Parses a Microsoft Word DOCX document file into an in-memory <see cref="DocumentModel"/>.
		/// </summary>
		/// <param name="wordFilePath">The absolute or relative file path to the input DOCX file. Cannot be null.</param>
		/// <returns>A populated <see cref="DocumentModel"/> representing the parsed structure.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="wordFilePath"/> is null.</exception>
		/// <exception cref="FileNotFoundException">Thrown when <paramref name="wordFilePath"/> does not exist.</exception>
		public static DocumentModel Parse(string wordFilePath) {
			if (wordFilePath == null) throw new ArgumentNullException(nameof(wordFilePath));

			if (!File.Exists(wordFilePath)) {
				throw new FileNotFoundException($"Input document not found: '{wordFilePath}'");
			}

			using WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, false);
			return DocxParser.Parse(wordDoc);
		}

		/// <summary>
		/// Converts a DOCX file at the specified input path into a rendered PDF file at the target output path.
		/// </summary>
		/// <param name="wordFilePath">The file path to the source DOCX document. Cannot be null.</param>
		/// <param name="outputPdfFilePath">The destination file path for the generated PDF document. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="wordFilePath"/> or <paramref name="outputPdfFilePath"/> is null.</exception>
		/// <exception cref="FileNotFoundException">Thrown when <paramref name="wordFilePath"/> does not exist.</exception>
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
