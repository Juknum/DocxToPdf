using System;
using DocxToPdf.Model;
using PdfSharp.Drawing;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Interface for rendering headers and footers onto PDF pages.
	/// </summary>
	public interface IHeaderFooterRenderer {
		/// <summary>
		/// Renders header elements for a section on the specified page number.
		/// </summary>
		/// <param name="section">SectionModel object.</param>
		/// <param name="pageNumber">1-indexed page number.</param>
		/// <param name="totalPages">Total document page count.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		void RenderHeader(SectionModel section, int pageNumber, int totalPages, XGraphics gfx);

		/// <summary>
		/// Renders footer elements for a section on the specified page number.
		/// </summary>
		/// <param name="section">SectionModel object.</param>
		/// <param name="pageNumber">1-indexed page number.</param>
		/// <param name="totalPages">Total document page count.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		void RenderFooter(SectionModel section, int pageNumber, int totalPages, XGraphics gfx);
	}
}
