using System;
using System.IO;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	public static class ImageRenderer {

		public static (double Width, double Height) MeasureDrawing(DrawingModel drawing, double containerWidth) {
			double width = drawing.WidthPt;
			double height = drawing.HeightPt;

			if ((width <= 0 || height <= 0) && drawing.ImageData.Length > 0) {
				try {
					using MemoryStream ms = new MemoryStream(drawing.ImageData);
					using XImage img = XImage.FromStream(ms);
					if (width <= 0) width = img.PixelWidth * 72.0 / 96.0; // Assume 96 DPI default if not set
					if (height <= 0) height = img.PixelHeight * 72.0 / 96.0;
				} catch {
					width = Math.Max(width, 100);
					height = Math.Max(height, 100);
				}
			}

			// Scale down inline drawings if wider than printable container width
			if (drawing.Placement == DrawingPlacement.Inline && width > containerWidth && width > 0) {
				double ratio = containerWidth / width;
				width = containerWidth;
				height *= ratio;
			}

			return (width, height);
		}

		/// <summary>
		/// Renders an OpenXML drawing model onto a PDFsharp XGraphics canvas.
		/// </summary>
		/// <param name="drawing">The drawing model to render.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="containerX">The paragraph container X origin in points.</param>
		/// <param name="currentY">The current paragraph anchor Y coordinate in points.</param>
		/// <param name="containerWidth">The printable width in points.</param>
		/// <returns>The height in points consumed by inline drawings.</returns>
		public static double RenderDrawing(DrawingModel drawing, XGraphics gfx, double containerX, ref double currentY, double containerWidth) {
			var (width, height) = MeasureDrawing(drawing, containerWidth);
			if (width <= 0) width = 100;
			if (height <= 0) height = 100;

			double x = CalculateX(drawing, containerX, containerWidth, width);
			double y = CalculateY(drawing, currentY, height);

			bool rendered = false;

			if (drawing.ImageData != null && drawing.ImageData.Length > 0) {
				try {
					using MemoryStream ms = new MemoryStream(drawing.ImageData);
					using XImage img = XImage.FromStream(ms);
					gfx.DrawImage(img, x, y, width, height);
					rendered = true;
				} catch (Exception ex) {
					string header = drawing.ImageData.Length >= 4 ? $"{drawing.ImageData[0]:X2}{drawing.ImageData[1]:X2}{drawing.ImageData[2]:X2}{drawing.ImageData[3]:X2}" : "";
					Console.WriteLine($"[ImageRenderer] Failed to render native image (ContentType={drawing.ContentType}, Bytes={drawing.ImageData.Length}, Header={header}): {ex.Message}. Attempting EmfRasterizer...");

					if (EmfRasterizer.RenderEmf(drawing.ImageData, gfx, x, y, width, height)) {
						rendered = true;
					}
				}
			}

			if ((drawing.ImageData == null || drawing.ImageData.Length == 0) && (!string.IsNullOrEmpty(drawing.FillColorHex) || !string.IsNullOrEmpty(drawing.BorderColorHex))) {
				XSolidBrush? fillBrush = null;
				if (!string.IsNullOrEmpty(drawing.FillColorHex)) {
					XColor fillColor = TextMeasurer.ParseColor(drawing.FillColorHex, XColors.Transparent);
					if (fillColor != XColors.Transparent && fillColor != XColors.White) {
						fillBrush = new XSolidBrush(fillColor);
					}
				}

				XPen? borderPen = null;
				if (!string.IsNullOrEmpty(drawing.BorderColorHex)) {
					XColor borderColor = TextMeasurer.ParseColor(drawing.BorderColorHex, XColors.Black);
					borderPen = new XPen(borderColor, 1);
				}

				if (fillBrush != null && borderPen != null) {
					gfx.DrawRectangle(borderPen, fillBrush, x, y, width, height);
					rendered = true;
				} else if (fillBrush != null) {
					gfx.DrawRectangle(fillBrush, x, y, width, height);
					rendered = true;
				} else if (borderPen != null) {
					gfx.DrawRectangle(borderPen, x, y, width, height);
					rendered = true;
				}
			}

			if (drawing.TextboxParagraphs.Count > 0) {
				double innerY = y + 2.0;
				double innerX = x + 2.0;
				double innerWidth = Math.Max(10.0, width - 4.0);

				foreach (var txbxP in drawing.TextboxParagraphs) {
					var tLayout = TextLayoutEngine.MeasureParagraph(txbxP, gfx, innerWidth, 1, 1);
					TextLayoutEngine.RenderParagraph(tLayout, gfx, innerX, ref innerY);
				}
				rendered = true;
			}

			if (drawing.Placement == DrawingPlacement.Inline && rendered) {
				currentY += height + 6.0;
				return height + 6.0;
			}

			return 0;
		}

		/// <summary>
		/// Calculates the exact X coordinate in PDF points for a drawing model based on OpenXML relativeFrom reference frames and alignment properties.
		/// </summary>
		/// <param name="drawing">The drawing model with positioning properties.</param>
		/// <param name="containerX">The paragraph content container X origin in points.</param>
		/// <param name="containerWidth">The printable page area width in points.</param>
		/// <param name="width">The measured drawing width in points.</param>
		/// <returns>The calculated X coordinate in PDF points.</returns>
		public static double CalculateX(DrawingModel drawing, double containerX, double containerWidth, double width) {
			if (drawing.Placement == DrawingPlacement.Inline) {
				return containerX + drawing.OffsetXPt;
			}

			string relH = drawing.HorizontalRelativeFrom?.ToLowerInvariant() ?? "column";

			if (!string.IsNullOrEmpty(drawing.AlignH)) {
				double refX = containerX;
				double refW = containerWidth;
				if (relH == "page") {
					refX = 0;
					refW = 612.0;
				}
				return drawing.AlignH switch {
					"left" => refX,
					"center" => refX + (refW - width) / 2.0,
					"right" => refX + refW - width,
					_ => containerX + drawing.OffsetXPt
				};
			}

			if (relH == "page") {
				return drawing.OffsetXPt;
			} else if (relH == "margin" || relH == "leftmargin") {
				return 72.0 + drawing.OffsetXPt;
			} else if (relH == "rightmargin") {
				return (612.0 - 72.0) + drawing.OffsetXPt;
			}

			return containerX + drawing.OffsetXPt;
		}

		/// <summary>
		/// Calculates the exact Y coordinate in PDF points for a drawing model based on OpenXML relativeFrom reference frames and alignment properties.
		/// </summary>
		/// <param name="drawing">The drawing model with positioning properties.</param>
		/// <param name="currentY">The current paragraph anchor Y baseline coordinate in points.</param>
		/// <param name="height">The measured drawing height in points.</param>
		/// <returns>The calculated Y coordinate in PDF points.</returns>
		public static double CalculateY(DrawingModel drawing, double currentY, double height) {
			if (drawing.Placement == DrawingPlacement.Inline) {
				return currentY;
			}

			string relV = drawing.VerticalRelativeFrom?.ToLowerInvariant() ?? "paragraph";

			if (!string.IsNullOrEmpty(drawing.AlignV)) {
				double refY = 72.0;
				double refH = 792.0 - 144.0;
				if (relV == "page") {
					refY = 0;
					refH = 792.0;
				}
				return drawing.AlignV switch {
					"top" => refY,
					"center" => refY + (refH - height) / 2.0,
					"bottom" => refY + refH - height,
					_ => currentY + drawing.OffsetYPt
				};
			}

			if (relV == "page") {
				return drawing.OffsetYPt;
			} else if (relV == "margin" || relV == "topmargin") {
				return 72.0 + drawing.OffsetYPt;
			} else if (relV == "bottommargin") {
				return (792.0 - 72.0) + drawing.OffsetYPt;
			}

			return currentY + drawing.OffsetYPt;
		}
	}
}
