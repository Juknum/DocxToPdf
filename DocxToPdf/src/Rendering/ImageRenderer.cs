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

			// Scale down if wider than printable container width
			if (width > containerWidth && width > 0) {
				double ratio = containerWidth / width;
				width = containerWidth;
				height *= ratio;
			}

			return (width, height);
		}

		public static double RenderDrawing(DrawingModel drawing, XGraphics gfx, double containerX, ref double currentY, double containerWidth) {
			if (drawing.ImageData == null || drawing.ImageData.Length == 0) {
				return 0;
			}

			var (width, height) = MeasureDrawing(drawing, containerWidth);
			if (width <= 0) width = 100;
			if (height <= 0) height = 100;

			double x = containerX + drawing.OffsetXPt;
			double y = (drawing.Placement == DrawingPlacement.Floating) ? (currentY + drawing.OffsetYPt) : currentY;

			bool rendered = false;
			try {
				using MemoryStream ms = new MemoryStream(drawing.ImageData);
				using XImage img = XImage.FromStream(ms);
				gfx.DrawImage(img, x, y, width, height);
				rendered = true;
			} catch (Exception ex) {
				Console.WriteLine($"[ImageRenderer] Failed to render image: {ex.Message}. Using vector box fallback.");
				// Render graceful fallback bounding box for unsupported format (e.g. EMF/WMF)
				XPen borderPen = new XPen(XColor.FromArgb(200, 200, 200), 1);
				XSolidBrush bgBrush = new XSolidBrush(XColor.FromArgb(248, 249, 250));
				gfx.DrawRectangle(borderPen, bgBrush, x, y, width, height);
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
