using System;
using System.IO;
using DocxToPdf;

namespace ConsoleApp {
	internal class Program {
		private static void Main(string[] args) {
			if (args.Length > 0 && args[0].Equals("pdf2img", StringComparison.OrdinalIgnoreCase)) {
				string pdfPath = args.Length > 1 ? args[1] : "DocxToPdf.Files/cover-out.pdf";
				string outDir = args.Length > 2 ? args[2] : "DocxToPdf.Files/pdf_images";
				PdfToImageConverter.ConvertPdfToImages(pdfPath, outDir);
				return;
			}

			if (args.Length > 0 && args[0].Equals("compare", StringComparison.OrdinalIgnoreCase)) {
				string expectedPdf = args.Length > 1 ? args[1] : "DocxToPdf.Files/cover-expected.pdf";
				string actualPdf = args.Length > 2 ? args[2] : "DocxToPdf.Files/cover-out.pdf";
				string outDir = args.Length > 3 ? args[3] : "DocxToPdf.Files/comparison_images";
				PdfToImageConverter.ComparePdfs(expectedPdf, actualPdf, outDir);
				return;
			}

			string inputPath = args.Length > 0 ? args[0] : "DocxToPdf.Files/cover.docx";
			string outputPath = args.Length > 1 ? args[1] : "DocxToPdf.Files/cover-out.pdf";

			if (File.Exists(inputPath)) {
				Converter.Convert(inputPath, outputPath);
			} else {
				Console.WriteLine("Usage:");
				Console.WriteLine("  dotnet run --project ConsoleApp -- [input.docx] [output.pdf]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- pdf2img [input.pdf] [output_dir]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- compare [expected.pdf] [actual.pdf] [output_dir]");
				Console.WriteLine($"Input file '{inputPath}' does not exist.");
			}
		}
	}
}


