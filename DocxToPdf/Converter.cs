using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Spire.Doc;

namespace DocxToPdf {
	public class Converter {

		private const int MAX_PAGES_PER_PART_ALLOWED = 3;

		private static int GetRequiredPartsCount(int pageCount) {
			return pageCount % MAX_PAGES_PER_PART_ALLOWED == 0 
				? pageCount / MAX_PAGES_PER_PART_ALLOWED
				: (pageCount / MAX_PAGES_PER_PART_ALLOWED) + 1;
		}

		private static void MergePdfFiles(string[] pdfFilePaths, string outputPdfFilePath) {
			Console.WriteLine($"Merging the parts into '{outputPdfFilePath}'...");

			PdfDocument outputPdf = new();
			foreach (string pdfFilePath in pdfFilePaths) {
				PdfDocument partialPdf = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.Import);
				outputPdf.Version = partialPdf.Version;

				foreach (PdfPage page in partialPdf.Pages) {
					outputPdf.AddPage(page);
				}

				partialPdf.Close();
				File.Delete(pdfFilePath);
			}

			outputPdf.Save(outputPdfFilePath);
			Console.WriteLine("Merging completed.");
		}

		public static void Convert(string wordFilePath, string outputPdfFilePath) {
		
			Document document = new();
			document.LoadFromFile(wordFilePath);

			Console.WriteLine($"Converting '{wordFilePath}' to PDF...");
			Console.WriteLine($"This document has {document.PageCount} pages.");

			if (document.PageCount > MAX_PAGES_PER_PART_ALLOWED) {
				Console.WriteLine("Splitting the document into multiple parts...");
				
				int parts = GetRequiredPartsCount(document.PageCount);
				string[] tmpPaths = new string[parts];
				
				Console.WriteLine($"The document will be split into '{parts}' parts.");

				for	(int i = 0; i < parts; i++) {
					int first = i * MAX_PAGES_PER_PART_ALLOWED;
					int count = Math.Min(MAX_PAGES_PER_PART_ALLOWED, document.PageCount - first);

					tmpPaths[i] = Path.GetTempFileName();

					var extracted = document.ExtractPages(first, count);
					extracted.SaveToFile(tmpPaths[i], FileFormat.PDF);
					extracted.Dispose();

					Console.WriteLine($"Created '{tmpPaths[i]}'");
				}

				MergePdfFiles(tmpPaths, outputPdfFilePath);
			}

			else document.SaveToFile(outputPdfFilePath, FileFormat.PDF);

			document.Dispose();
		}
	}
}
