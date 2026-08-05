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

			try {
				using MemoryStream ms = new MemoryStream(drawing.ImageData);
				using XImage img = XImage.FromStream(ms);

				double x = containerX + drawing.OffsetXPt;
				double y = (drawing.Placement == DrawingPlacement.Floating) ? (currentY + drawing.OffsetYPt) : currentY;

				gfx.DrawImage(img, x, y, width, height);

				if (drawing.Placement == DrawingPlacement.Inline) {
					currentY += height + 6.0; // 6pt bottom spacing for inline image
					return height + 6.0;
				}
			} catch (Exception ex) {
				Console.WriteLine($"[ImageRenderer] Failed to render image: {ex.Message}");
			}

			return 0;
		}
	}
}
