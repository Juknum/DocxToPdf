using System;
using System.IO;
using System.Linq;
using DocxToPdf;
using DocxToPdf.Model;

namespace ConsoleApp {
	internal class Program {
		private static void Main(string[] args) {
			if (args.Length == 0) {
				Console.WriteLine("DocxToPdf Console & E2E Tool");
				Console.WriteLine("Usage:");
				Console.WriteLine("  dotnet run --project ConsoleApp -- [input.docx] [output.pdf]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- pdf2img [input.pdf] [output_dir]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- compare [expected.pdf] [actual.pdf] [output_dir]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- verify [files_dir]");
				return;
			}

			if (args.Length >= 1 && args[0].Equals("verify", StringComparison.OrdinalIgnoreCase)) {
				string filesDir = args.Length > 1 ? args[1] : "DocxToPdf.Files";
				bool success = PdfToImageConverter.VerifyDocxToPdfFiles(filesDir);
				Environment.Exit(success ? 0 : 1);
				return;
			}

			if (args.Length >= 3 && args[0].Equals("pdf2img", StringComparison.OrdinalIgnoreCase)) {
				string pdfPath = args[1];
				string outDir  = args[2];

				PdfToImageConverter.ConvertPdfToImages(pdfPath, outDir);
				return;
			}

			if (args.Length >= 3 && args[0].Equals("compare", StringComparison.OrdinalIgnoreCase)) {
				string expectedPdf = args[1];
				string actualPdf   = args[2];
				string outDir = args.Length > 3 ? args[3] : "DocxToPdf.Files/comparison_images";
				PdfToImageConverter.ComparePdfs(expectedPdf, actualPdf, outDir);
				return;
			}

			if (args.Length >= 2) {
				string inputDocx = args[0];
				string outputPdf = args[1];
				Converter.Convert(inputDocx, outputPdf);
				return;
			}

			Console.WriteLine("Invalid arguments provided.");
		}
	}
}
