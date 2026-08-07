using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;

namespace ConsoleApp {
	/// <summary>
	/// Utility helper class for converting PDF documents into PNG page images and performing pixel similarity comparison.
	/// </summary>
	public static class PdfToImageConverter {
		/// <summary>
		/// Converts all pages of a PDF document into high-resolution PNG images.
		/// </summary>
		/// <param name="pdfPath">Path to PDF document file. Cannot be null.</param>
		/// <param name="outputDir">Target output directory. Cannot be null.</param>
		/// <param name="filePrefix">Output image filename prefix.</param>
		/// <param name="dpi">Rendering resolution DPI.</param>
		/// <param name="scale">Page dimension scale factor.</param>
		/// <returns>List of generated PNG image file paths.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="pdfPath"/> or <paramref name="outputDir"/> is null.</exception>
		/// <exception cref="FileNotFoundException">Thrown when <paramref name="pdfPath"/> does not exist.</exception>
		public static List<string> ConvertPdfToImages(string pdfPath, string outputDir, string filePrefix = "page", int dpi = 150, double scale = 0.5) {
			if (pdfPath == null) throw new ArgumentNullException(nameof(pdfPath));
			if (outputDir == null) throw new ArgumentNullException(nameof(outputDir));

			if (!File.Exists(pdfPath)) {
				throw new FileNotFoundException($"PDF file not found: '{pdfPath}'");
			}

			if (!Directory.Exists(outputDir)) {
				Directory.CreateDirectory(outputDir);
			}

			List<string> generatedImages = new();
			double dimFactor = (dpi * scale) / 72.0;

			using (var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(dimFactor))) {
				int pageCount = docReader.GetPageCount();
				Console.WriteLine($"Rendering {pageCount} page(s) from '{pdfPath}' at {dpi} DPI to '{outputDir}'...");

				for (int i = 0; i < pageCount; i++) {
					using var pageReader = docReader.GetPageReader(i);
					int width = pageReader.GetPageWidth();
					int height = pageReader.GetPageHeight();
					byte[] rawBytes = pageReader.GetImage();

					var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
					using var rawBitmap = new SKBitmap(info);
					Marshal.Copy(rawBytes, 0, rawBitmap.GetPixels(), rawBytes.Length);

					using var flattenedBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
					using (var canvas = new SKCanvas(flattenedBitmap)) {
						canvas.Clear(SKColors.White);
						canvas.DrawBitmap(rawBitmap, 0, 0);
					}

					string outPath = Path.Combine(outputDir, $"{filePrefix}_page_{i + 1}.png");
					using var image = SKImage.FromBitmap(flattenedBitmap);
					using var data = image.Encode(SKEncodedImageFormat.Png, 100);
					using var stream = File.Create(outPath);
					data.SaveTo(stream);

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

		/// <summary>
		/// Verifies each sample directory in DocxToPdf.Files by converting input.docx to output.pdf
		/// and comparing extracted page images against expected.pdf using a 90% threshold.
		/// </summary>
		public static bool VerifyDocxToPdfFiles(string baseDir = "DocxToPdf.Files", double thresholdPercent = 90.0) {
			if (!Directory.Exists(baseDir)) {
				Console.WriteLine($"Base directory '{baseDir}' not found.");
				return false;
			}

			string[] sampleDirs = Directory.GetDirectories(baseDir);
			bool allPassed = true;

			foreach (string sampleDir in sampleDirs) {
				string folderName = Path.GetFileName(sampleDir);
				if (folderName.Equals("comparison_images", StringComparison.OrdinalIgnoreCase) ||
				    folderName.Equals("pdf_images", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				string inputDocx = Path.Combine(sampleDir, "input.docx");
				string expectedPdf = Path.Combine(sampleDir, "expected.pdf");
				string outputPdf = Path.Combine(sampleDir, "output.pdf");

				if (!File.Exists(inputDocx) || !File.Exists(expectedPdf)) {
					continue;
				}

				Console.WriteLine($"\n--- Verifying E2E Sample: {folderName} ---");
				DocxToPdf.Converter.Convert(inputDocx, outputPdf);

				var expectedImgs = ConvertPdfToImages(expectedPdf, sampleDir, "expected");
				var outputImgs = ConvertPdfToImages(outputPdf, sampleDir, "output");

				if (expectedImgs.Count != outputImgs.Count) {
					Console.WriteLine($"❌ Page count mismatch for {folderName}: expected {expectedImgs.Count}, got {outputImgs.Count}");
					allPassed = false;
					continue;
				}

				for (int i = 0; i < expectedImgs.Count; i++) {
					using var img1 = SKBitmap.Decode(expectedImgs[i]);
					using var img2 = SKBitmap.Decode(outputImgs[i]);

					double similarity = CompareImages(img1, img2);
					double pct = similarity * 100.0;

					if (pct >= thresholdPercent) {
						Console.WriteLine($"✅ [{folderName}] Page {i + 1}: {pct:F2}% match (>= {thresholdPercent:F1}%)");
					} else {
						Console.WriteLine($"❌ [{folderName}] Page {i + 1}: {pct:F2}% match (< {thresholdPercent:F1}% threshold)");
						allPassed = false;
					}
				}
			}

			return allPassed;
		}

		/// <summary>
		/// Compares two SKBitmap images pixel by pixel within a color tolerance threshold.
		/// </summary>
		/// <param name="img1">First bitmap image. Cannot be null.</param>
		/// <param name="img2">Second bitmap image. Cannot be null.</param>
		/// <param name="colorTolerance">Color channel RGB difference tolerance.</param>
		/// <returns>Similarity score between 0.0 (0% match) and 1.0 (100% match).</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="img1"/> or <paramref name="img2"/> is null.</exception>
		public static double CompareImages(SKBitmap img1, SKBitmap img2, byte colorTolerance = 30) {
			if (img1 == null) throw new ArgumentNullException(nameof(img1));
			if (img2 == null) throw new ArgumentNullException(nameof(img2));
			int minWidth = Math.Min(img1.Width, img2.Width);
			int minHeight = Math.Min(img1.Height, img2.Height);
			int maxWidth = Math.Max(img1.Width, img2.Width);
			int maxHeight = Math.Max(img1.Height, img2.Height);
			int totalPixels = maxWidth * maxHeight;

			if (totalPixels == 0) return 1.0;

			long matchingPixels = 0;

			for (int y = 0; y < maxHeight; y++) {
				for (int x = 0; x < minWidth; x++) {
					if (y < minHeight) {
						SKColor p1 = img1.GetPixel(x, y);
						SKColor p2 = img2.GetPixel(x, y);

						var (r1, g1, b1) = ToCompositeRgb(p1);
						var (r2, g2, b2) = ToCompositeRgb(p2);

						int rDiff = Math.Abs(r1 - r2);
						int gDiff = Math.Abs(g1 - g2);
						int bDiff = Math.Abs(b1 - b2);

						if (rDiff <= colorTolerance && gDiff <= colorTolerance && bDiff <= colorTolerance) {
							matchingPixels++;
						}
					}
				}
			}

			return (double)matchingPixels / totalPixels;
		}

		private static (byte R, byte G, byte B) ToCompositeRgb(SKColor p) {
			if (p.Alpha == 0) return (255, 255, 255);
			int a = p.Alpha;
			byte r = (byte)((p.Red * a + 255 * (255 - a)) / 255);
			byte g = (byte)((p.Green * a + 255 * (255 - a)) / 255);
			byte b = (byte)((p.Blue * a + 255 * (255 - a)) / 255);
			return (r, g, b);
		}
	}
}
