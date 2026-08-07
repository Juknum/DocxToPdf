using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
#if !NET48
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif
using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;
using DocxToPdf.Model;

namespace DocxToPdf.Tests {
	[Trait("Category", "E2E")]
	[Collection("E2ETests")]
	public class E2EWorkflowTests {
		private readonly ITestOutputHelper _output;
		private static readonly object FileLock = new object();

		public E2EWorkflowTests(ITestOutputHelper output) {
			_output = output;
		}

		public static string FindDocxToPdfFilesDirectory() {
			string current = AppContext.BaseDirectory;
			while (!string.IsNullOrEmpty(current)) {
				string candidate = Path.Combine(current, "DocxToPdf.Files");
				if (Directory.Exists(candidate)) {
					return candidate;
				}
				string? parent = Directory.GetParent(current)?.FullName;
				if (parent == null || parent == current) break;
				current = parent;
			}
			throw new DirectoryNotFoundException("Could not find 'DocxToPdf.Files' directory.");
		}

		private static string GetScaleTag(double scale) {
			return scale switch {
				0.125 => "0125",
				0.25 => "0250",
				0.5 => "0500",
				0.75 => "0750",
				1.0 => "1000",
				_ => scale.ToString("0.###").Replace(".", "_")
			};
		}

		private static string GetScorecardBasePath(string sampleDir) {
			return Path.Combine(sampleDir, "e2e-scorecard");
		}

		private static string GetScaleVerificationChecklist(double scale, double threshold, string scaleTag) {
			var checks = new List<string>();

			if (scale <= 0.125) {
				checks.Add("start with presence/absence checks for major elements, images, headers, footers, links...");
				checks.Add("confirm nothing is missing entirely or is being missplaced before looking at finer visual detail");
			} else if (scale <= 0.25) {
				checks.Add("look for misaligned blocks, shifted margins, wrapping changes, and table structure drift");
				checks.Add("check whether text blocks and images still sit in the expected positions");
			} else if (scale <= 0.5) {
				checks.Add("check color fidelity, fills, borders, and image rendering differences");
				checks.Add("inspect whether visual styling changes are real or caused by scaling artifacts");
			} else if (scale <= 0.75) {
				checks.Add("review spacing, typography, line breaks, and page-break placement");
				checks.Add("confirm headers, footers, and repeated elements remain consistent across pages");
			} else {
				checks.Add("look for final visual drift such as subtle layout shifts, font differences, or color mismatches");
				checks.Add("use this scale to confirm the whole page closely matches the expected PDF");
			}

			checks.Add($"if a failure occurs, open diff_page_s{scaleTag}_*.png for the first mismatched page and compare it against expected/output images");
			checks.Add($"verify each page match is >= {threshold:F2}% and review the generated e2e-scorecard JSON/MD for the sample");

			return $"Scale {scale:0.###} checklist: " + string.Join("; ", checks);
		}

		private static void WriteScorecardArtifacts(string sampleDir, E2EScorecardReport report) {
			string basePath = GetScorecardBasePath(sampleDir);
			File.WriteAllText(basePath + ".json", SerializeScorecardReport(report));
			File.WriteAllText(basePath + ".md", BuildScorecardMarkdown(report));
		}

		private static string SerializeScorecardReport(E2EScorecardReport report) {
#if NET48
			return JsonConvert.SerializeObject(report, Formatting.Indented);
#else
			var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
			return JsonSerializer.Serialize(report, jsonOptions);
#endif
		}

		private static string BuildScorecardMarkdown(E2EScorecardReport report) {
			var lines = new List<string> {
				$"# E2E Scorecard - {report.SampleName}",
				$"- Verdict: {(report.Passed ? "PASS" : "FAIL")}",
				$"- Generated at (UTC): {report.GeneratedAtUtc:O}",
				string.Empty,
				"| Scale | Threshold | Expected Pages | Output Pages | Avg Match | Worst Match | Verdict |",
				"| --- | --- | ---: | ---: | ---: | ---: | --- |"
			};

			foreach (E2EScaleScorecard scale in report.Scales) {
				string average = scale.AverageMatchPercent.HasValue ? scale.AverageMatchPercent.Value.ToString("F2") + "%" : "n/a";
				string worst = scale.WorstMatchPercent.HasValue ? scale.WorstMatchPercent.Value.ToString("F2") + "%" : "n/a";
				lines.Add($"| {scale.Scale:0.###} | {scale.Threshold:F2}% | {scale.ExpectedPages} | {scale.OutputPages} | {average} | {worst} | {(scale.Passed ? "PASS" : "FAIL")} |");
				foreach (E2EScaleFailure failure in scale.Failures) {
					lines.Add($"  - {failure.Message}");
				}
			}

			return string.Join(Environment.NewLine, lines);
		}

		[Fact]
		public void VerifyAllDocxToPdfFilesE2E() {
			lock (FileLock) {
				(double scale, double threshold)[] verificationSteps = new[] {
					(0.500, 85.0)
				};

				string filesDir = FindDocxToPdfFilesDirectory();
				_output.WriteLine($"DocxToPdf.Files directory found: {filesDir}");

				string[] sampleDirectories = Directory.GetDirectories(filesDir);
				var samples = new List<(string SampleDir, string FolderName, string InputDocx, string ExpectedPdf, string OutputPdf)>();

				foreach (string sampleDir in sampleDirectories) {
					string folderName = Path.GetFileName(sampleDir);
					if (folderName.Equals("comparison_images", StringComparison.OrdinalIgnoreCase) ||
					    folderName.Equals("pdf_images", StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					string inputDocx = Path.Combine(sampleDir, "input.docx");
					string expectedPdf = Path.Combine(sampleDir, "expected.pdf");
					string outputPdf = Path.Combine(sampleDir, "output.pdf");

					if (!File.Exists(inputDocx) || !File.Exists(expectedPdf)) {
						_output.WriteLine($"Skipping directory '{folderName}' because input.docx or expected.pdf is missing.");
						continue;
					}

					_output.WriteLine($"=== Processing E2E Sample: {folderName} ===");
					DocumentModel parsedDoc = Converter.Parse(inputDocx);
					_output.WriteLine($"Parsed {folderName}: {parsedDoc.Sections.Count} section(s)");
					for (int s = 0; s < parsedDoc.Sections.Count; s++) {
						var sec = parsedDoc.Sections[s];
						_output.WriteLine($"  Section {s + 1}: {sec.Elements.Count} element(s). HeaderDefault? {sec.HeaderDefault != null}, FooterDefault? {sec.FooterDefault != null}");
					}

					Converter.Convert(inputDocx, outputPdf);
					Assert.True(File.Exists(outputPdf), $"output.pdf was not generated for {folderName}");

					samples.Add((sampleDir, folderName, inputDocx, expectedPdf, outputPdf));
				}

				Assert.NotEmpty(samples);

				var sampleScorecards = new Dictionary<string, List<E2EScaleScorecard>>();
				var samplePasses = new Dictionary<string, bool>();
				foreach (var sample in samples) {
					sampleScorecards[sample.SampleDir] = new List<E2EScaleScorecard>();
					samplePasses[sample.SampleDir] = true;
				}

				var allFailures = new List<string>();

				foreach ((double scale, double threshold) in verificationSteps) {
					string scaleTag = GetScaleTag(scale);
					_output.WriteLine($"=== Verifying scale {scale:0.###} with threshold {threshold:F2}% ===");
					_output.WriteLine(GetScaleVerificationChecklist(scale, threshold, scaleTag));

					foreach (var sample in samples) {
						_output.WriteLine($"[{sample.FolderName}] Verifying at scale {scale:0.###} with threshold {threshold:F2}%");
						var pageScorecards = new List<E2EPageScorecard>();
						var currentScaleFailures = new List<E2EScaleFailure>();

						List<string> expectedImages = RenderPdfToImages(sample.ExpectedPdf, sample.SampleDir, $"expected_page_s{scaleTag}", scale: scale);
						List<string> outputImages = RenderPdfToImages(sample.OutputPdf, sample.SampleDir, $"output_page_s{scaleTag}", scale: scale);

						_output.WriteLine($"expected.pdf page count: {expectedImages.Count}, output.pdf page count: {outputImages.Count}");

						if (expectedImages.Count == 0) {
							currentScaleFailures.Add(new E2EScaleFailure($"expected.pdf in {sample.FolderName} has 0 pages at scale {scale:0.###}."));
						}

						if (expectedImages.Count != outputImages.Count) {
							currentScaleFailures.Add(new E2EScaleFailure($"[{sample.FolderName}] Scale {scale:0.###} page count mismatch: expected {expectedImages.Count}, output {outputImages.Count}."));
						}

						int compareCount = Math.Min(expectedImages.Count, outputImages.Count);
						double totalMatchPercent = 0.0;
						double worstMatchPercent = double.MaxValue;
						for (int i = 0; i < compareCount; i++) {
							using var expectedImg = SKBitmap.Decode(expectedImages[i]);
							using var outputImg = SKBitmap.Decode(outputImages[i]);

							string diffPath = Path.Combine(sample.SampleDir, $"diff_page_s{scaleTag}_{i + 1}.png");
							double similarity = CompareAndGenerateDiffImage(expectedImg, outputImg, diffPath, _output);
							double percentage = similarity * 100.0;
							bool pagePassed = percentage >= threshold;
							pageScorecards.Add(new E2EPageScorecard(i + 1, percentage, pagePassed, diffPath));
							totalMatchPercent += percentage;
							worstMatchPercent = Math.Min(worstMatchPercent, percentage);

							_output.WriteLine($"[{sample.FolderName}] Scale {scale:0.###} Page {i + 1}: Match = {percentage:F2}% (Threshold = {threshold:F2}%)");
							if (!pagePassed) {
								currentScaleFailures.Add(new E2EScaleFailure($"[{sample.FolderName}] Scale {scale:0.###} Page {i + 1} match score ({percentage:F2}%) is below {threshold:F2}% threshold!"));
							}
						}

						var scaleReport = new E2EScaleScorecard(
							scale,
							threshold,
							expectedImages.Count,
							outputImages.Count,
							compareCount > 0 ? totalMatchPercent / compareCount : (double?)null,
							compareCount > 0 ? worstMatchPercent : (double?)null,
							currentScaleFailures.Count == 0,
							pageScorecards,
							currentScaleFailures
						);
						sampleScorecards[sample.SampleDir].Add(scaleReport);
						samplePasses[sample.SampleDir] = samplePasses[sample.SampleDir] && currentScaleFailures.Count == 0;
						WriteScorecardArtifacts(sample.SampleDir, new E2EScorecardReport(sample.FolderName, DateTime.UtcNow, samplePasses[sample.SampleDir], sampleScorecards[sample.SampleDir]));

						if (currentScaleFailures.Count > 0) {
							allFailures.AddRange(currentScaleFailures.ConvertAll(f => f.Message));
						}
					}
				}

				if (allFailures.Count > 0) {
					Assert.Fail(string.Join(Environment.NewLine, allFailures));
				}

				_output.WriteLine($"Successfully verified {samples.Count} E2E sample folder(s): {string.Join(", ", samples.ConvertAll(sample => sample.FolderName))}");
			}
		}

		public static List<string> RenderPdfToImages(string pdfPath, string outputDir, string filePrefix, int dpi = 150, double scale = 0.125) {
			List<string> generatedImages = new();
			double dimFactor = (dpi * scale) / 72.0;

			using (var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(dimFactor))) {
				int pageCount = docReader.GetPageCount();

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

					string outPath = Path.Combine(outputDir, $"{filePrefix}_{i + 1}.png");
					using var image = SKImage.FromBitmap(flattenedBitmap);
					using var data = image.Encode(SKEncodedImageFormat.Png, 100);
					using var stream = File.Create(outPath);
					data.SaveTo(stream);

					generatedImages.Add(outPath);
				}
			}

			return generatedImages;
		}

		public static double CompareAndGenerateDiffImage(SKBitmap img1, SKBitmap img2, string diffOutputPath, ITestOutputHelper? log = null, byte colorTolerance = 30) {
			int minWidth = Math.Min(img1.Width, img2.Width);
			int minHeight = Math.Min(img1.Height, img2.Height);
			int maxWidth = Math.Max(img1.Width, img2.Width);
			int maxHeight = Math.Max(img1.Height, img2.Height);
			int totalPixels = maxWidth * maxHeight;

			if (totalPixels == 0) return 1.0;

			long matchingPixels = 0;
			using var diffImg = new SKBitmap(maxWidth, maxHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);

			int mismatchSampleCount = 0;

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
							diffImg.SetPixel(x, y, new SKColor(r1, g1, b1, 255));
						} else {
							diffImg.SetPixel(x, y, new SKColor(0, 0, 255, 255));
							if (mismatchSampleCount < 20 && log != null) {
								log.WriteLine($"Mismatch at ({x},{y}): expected RGB=({r1},{g1},{b1}) [orig A={p1.Alpha}], output RGB=({r2},{g2},{b2}) [orig A={p2.Alpha}]");
								mismatchSampleCount++;
							}
						}
					} else {
						diffImg.SetPixel(x, y, new SKColor(0, 0, 255, 255));
					}
				}
			}

			if (log != null) {
				log.WriteLine($"Image sizes: img1 (expected) = {img1.Width}x{img1.Height}, img2 (output) = {img2.Width}x{img2.Height}");
				log.WriteLine($"Sample expected colors: TopLeft(0,0)={img1.GetPixel(0,0)}, Center({img1.Width/2},{img1.Height/2})={img1.GetPixel(img1.Width/2, img1.Height/2)}, BottomRight({img1.Width-1},{img1.Height-1})={img1.GetPixel(img1.Width-1, img1.Height-1)}");
				log.WriteLine($"Sample output colors: TopLeft(0,0)={img2.GetPixel(0,0)}, Center({img2.Width/2},{img2.Height/2})={img2.GetPixel(img2.Width/2, img2.Height/2)}, BottomRight({img2.Width-1},{img2.Height-1})={img2.GetPixel(img2.Width-1, img2.Height-1)}");
			}

			using var image = SKImage.FromBitmap(diffImg);
			using var data = image.Encode(SKEncodedImageFormat.Png, 100);
			using var stream = File.Create(diffOutputPath);
			data.SaveTo(stream);

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

		private sealed record E2EScorecardReport(string SampleName, DateTime GeneratedAtUtc, bool Passed, IReadOnlyList<E2EScaleScorecard> Scales);

		private sealed record E2EScaleScorecard(
			double Scale,
			double Threshold,
			int ExpectedPages,
			int OutputPages,
			double? AverageMatchPercent,
			double? WorstMatchPercent,
			bool Passed,
			IReadOnlyList<E2EPageScorecard> Pages,
			IReadOnlyList<E2EScaleFailure> Failures);

		private sealed record E2EPageScorecard(int PageNumber, double MatchPercent, bool Passed, string DiffPath);

		private sealed record E2EScaleFailure(string Message);
	}
}
