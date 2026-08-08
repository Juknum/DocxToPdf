using System;
using DocxToPdf.Model;
using PdfSharp.Drawing;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Interface for line breaking, text measurement, and paragraph rendering operations.
	/// </summary>
	public interface ITextLayoutEngine {
		/// <summary>
		/// Measures text runs, breaks lines, and resolves paragraph layout bounding dimensions.
		/// </summary>
		/// <param name="paragraph">ParagraphModel object.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		/// <param name="previousSpacingAfter">Spacing after previous paragraph in points.</param>
		/// <param name="currentPage">Current 1-indexed page number.</param>
		/// <param name="totalPages">Total document page count.</param>
		/// <returns>A populated <see cref="ParagraphLayout"/> object.</returns>
		ParagraphLayout MeasureParagraph(ParagraphModel paragraph, XGraphics gfx, double containerWidth, double previousSpacingAfter = 0, int currentPage = 1, int totalPages = 1);

		/// <summary>
		/// Renders a measured paragraph layout onto the graphics context canvas.
		/// </summary>
		/// <param name="layout">ParagraphLayout object.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="containerX">Left margin origin in points.</param>
		/// <param name="currentY">Ref top Y coordinate in points updated as lines are drawn.</param>
		void RenderParagraph(ParagraphLayout layout, XGraphics gfx, double containerX, ref double currentY);
	}
}
