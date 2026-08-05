using System;
using System.Collections.Generic;
using System.IO;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ConsoleApp {
	public static class PdfToImageConverter {
		/// <summary>
		/// Converts all pages of a PDF document into high-resolution PNG images.
		/// </summary>
		public static List<string> ConvertPdfToImages(string pdfPath, string outputDir, string filePrefix = "page", int dpi = 150) {
			if (!File.Exists(pdfPath)) {
				throw new FileNotFoundException($"PDF file not found: '{pdfPath}'");
			}

			if (!Directory.Exists(outputDir)) {
				Directory.CreateDirectory(outputDir);
			}

			List<string> generatedImages = new();
			double dimFactor = dpi / 72.0;

			using (var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(dimFactor))) {
				int pageCount = docReader.GetPageCount();
				Console.WriteLine($"Rendering {pageCount} page(s) from '{pdfPath}' at {dpi} DPI to '{outputDir}'...");

				for (int i = 0; i < pageCount; i++) {
					using var pageReader = docReader.GetPageReader(i);
					int width = pageReader.GetPageWidth();
					int height = pageReader.GetPageHeight();
					byte[] rawBytes = pageReader.GetImage();

					using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
					string outPath = Path.Combine(outputDir, $"{filePrefix}_page_{i + 1}.png");
					image.Save(outPath);
					generatedImages.Add(outPath);
					Console.WriteLine($"Saved page {i + 1}/{pageCount} -> '{outPath}' ({width}x{height} px)");
				}
			}

			return generatedImages;
		}

		/// <summary>
		/// Renders pages from expected and actual PDF files into images for side-by-side visual comparison.
		/// </summary>
		public static void ComparePdfs(string expectedPdf, string actualPdf, string outputDir, int dpi = 150) {
			Console.WriteLine($"=== PDF Page Image Comparison ===");
			Console.WriteLine($"Expected: {expectedPdf}");
			Console.WriteLine($"Actual:   {actualPdf}");

			var expectedImages = ConvertPdfToImages(expectedPdf, outputDir, "expected", dpi);
			var actualImages = ConvertPdfToImages(actualPdf, outputDir, "actual", dpi);

			Console.WriteLine($"Done! {expectedImages.Count} expected page image(s) and {actualImages.Count} actual page image(s) created in '{outputDir}'.");
		}
	}
}
