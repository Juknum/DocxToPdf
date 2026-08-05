using System;
using System.IO;
using System.Linq;
using DocxToPdf;
using DocxToPdf.Model;

namespace ConsoleApp {
	internal class Program {
		private static void Main(string[] args) {
			if (args.Length < 3) {
				Console.WriteLine("Not enough arguments provided. Please specify the command, input and output files.");
				Console.WriteLine("Usage:");
				Console.WriteLine("  dotnet run --project ConsoleApp -- [input.docx] [output.pdf]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- pdf2img [input.pdf] [output_dir]");
				Console.WriteLine("  dotnet run --project ConsoleApp -- compare [expected.pdf] [actual.pdf] [output_dir]");
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
		}
	}
}




