using System;
using DocxToPdf.Model;
using PdfSharp.Drawing;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Interface for measuring and rendering bitmap images and drawings in PDFsharp graphics contexts.
	/// </summary>
	public interface IImageRenderer {
		/// <summary>
		/// Measures drawing model width and height in points.
		/// </summary>
		/// <param name="drawing">DrawingModel object.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		/// <returns>A tuple of (Width, Height) in points.</returns>
		(double Width, double Height) MeasureDrawing(DrawingModel drawing, double containerWidth);

		/// <summary>
		/// Renders drawing image, vector shape, background shading, and textboxes.
		/// </summary>
		/// <param name="drawing">DrawingModel object.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="containerX">Printable area left X position in points.</param>
		/// <param name="currentY">Ref top Y coordinate in points updated as inline drawings are drawn.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		void RenderDrawing(DrawingModel drawing, XGraphics gfx, double containerX, ref double currentY, double containerWidth = 0);
	}
}
