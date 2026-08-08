using System;
using System.Collections.Generic;
using DocxToPdf.Model;
using PdfSharp.Drawing;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Interface for measuring and rendering table structures in PDFsharp graphics contexts.
	/// </summary>
	public interface ITableRenderer {
		/// <summary>
		/// Measures table layout, cell dimensions, and row heights for rendering.
		/// </summary>
		/// <param name="table">The table model to measure.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		/// <param name="currentPage">Current 1-indexed page number.</param>
		/// <param name="totalPages">Total document page count.</param>
		/// <returns>A list of measured <see cref="TableRowLayout"/> instances.</returns>
		List<TableRowLayout> MeasureTable(TableModel table, XGraphics gfx, double containerWidth, int currentPage = 1, int totalPages = 1);

		/// <summary>
		/// Renders a single measured table row into the graphics context.
		/// </summary>
		/// <param name="rowLayout">Measured TableRowLayout row object.</param>
		/// <param name="table">Parent TableModel instance.</param>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="containerX">Printable area left X position in points.</param>
		/// <param name="currentY">Top Y coordinate in points.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		void RenderRow(TableRowLayout rowLayout, TableModel table, XGraphics gfx, double containerX, double currentY, double containerWidth = 0);
	}
}
