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

		public static double RenderDrawing(DrawingModel drawing, XGraphics gfx, double containerX, ref double currentY, double containerWidth) {
			var (width, height) = MeasureDrawing(drawing, containerWidth);
			if (width <= 0) width = 100;
			if (height <= 0) height = 100;

			double x = containerX + drawing.OffsetXPt;
			if (drawing.Placement == DrawingPlacement.Floating) {
				if (!string.IsNullOrEmpty(drawing.HorizontalRelativeFrom) &&
					string.Equals(drawing.HorizontalRelativeFrom, "page", StringComparison.OrdinalIgnoreCase)) {
					x = drawing.OffsetXPt;
				} else if (drawing.BehindDoc && width >= 500) {
					x = 0;
				}
			}

			double y = currentY;
			if (drawing.Placement == DrawingPlacement.Floating) {
				if (drawing.BehindDoc && drawing.ImageData != null && drawing.ImageData.Length > 0 && drawing.WidthPt >= 500) {
					y = 0;
				} else if (!string.IsNullOrEmpty(drawing.VerticalRelativeFrom) &&
					string.Equals(drawing.VerticalRelativeFrom, "page", StringComparison.OrdinalIgnoreCase)) {
					y = drawing.OffsetYPt;
				} else {
					y = currentY + drawing.OffsetYPt;
				}
			}

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

	}
}
