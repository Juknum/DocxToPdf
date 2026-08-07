using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Represents measured layout metrics (X, Width, Height, Paragraphs) for a single table cell.
	/// </summary>
	public class TableCellLayout {
		/// <summary>Gets or sets the underlying cell model.</summary>
		public TableCellModel Cell { get; set; } = new();
		/// <summary>Gets or sets the cell X origin relative to table left edge.</summary>
		public double X { get; set; }
		/// <summary>Gets or sets the cell width in points.</summary>
		public double Width { get; set; }
		/// <summary>Gets or sets the cell height in points.</summary>
		public double Height { get; set; }
		/// <summary>Gets or sets measured paragraph layouts contained within the cell.</summary>
		public List<ParagraphLayout> Paragraphs { get; set; } = [];
	}

	/// <summary>
	/// Represents measured layout metrics (Cells, Height) for a single table row.
	/// </summary>
	public class TableRowLayout {
		/// <summary>Gets or sets the underlying row model.</summary>
		public TableRowModel Row { get; set; } = new();
		/// <summary>Gets or sets measured cell layouts in this row.</summary>
		public List<TableCellLayout> Cells { get; set; } = [];
		/// <summary>Gets or sets total row height in points.</summary>
		public double Height { get; set; }
		/// <summary>Gets whether this row is a header row.</summary>
		public bool IsHeader => Row.IsHeader;
	}

	/// <summary>
	/// Provides measurement and rendering logic for tables, rows, cells, borders, and background shading.
	/// </summary>
	public static class TableRenderer {

		/// <summary>
		/// Measures all row and cell heights in a table model given container width constraints.
		/// </summary>
		/// <param name="table">The table model. Cannot be null.</param>
		/// <param name="gfx">PDFsharp graphics context. Cannot be null.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		/// <param name="currentPage">Current 1-indexed page number.</param>
		/// <param name="totalPages">Total page count.</param>
		/// <returns>A list of measured <see cref="TableRowLayout"/> instances.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="table"/> or <paramref name="gfx"/> is null.</exception>
		public static List<TableRowLayout> MeasureTable(TableModel table, XGraphics gfx, double containerWidth, int currentPage = 1, int totalPages = 1) {
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));

			List<TableRowLayout> rowLayouts = [];

			// Resolve column widths
			List<double> colWidths = ResolveColumnWidths(table, containerWidth);

			foreach (var row in table.Rows) {
				TableRowLayout rowLayout = new TableRowLayout { Row = row };
				double colX = 0;
				int colIndex = 0;
				double maxCellHeight = row.HeightPt;

				foreach (var cell in row.Cells) {
					// Calculate span width
					double cellWidth = 0;
					int span = Math.Max(1, cell.GridSpan);
					for (int s = 0; s < span && (colIndex + s) < colWidths.Count; s++) {
						cellWidth += colWidths[colIndex + s];
					}
					if (cellWidth <= 0) cellWidth = containerWidth / Math.Max(1, row.Cells.Count);

					// Measure cell padding
					CellPaddingModel padding = MergePadding(cell.Padding, table.DefaultCellPadding);
					double innerWidth = Math.Max(1.0, cellWidth - padding.Left - padding.Right);

					TableCellLayout cellLayout = new TableCellLayout {
						Cell = cell,
						X = colX,
						Width = cellWidth
					};

					double cellContentHeight = padding.Top + padding.Bottom;

					// Measure cell block elements (paragraphs)
					foreach (var elem in cell.Elements) {
						if (elem is ParagraphModel p) {
							var pLayout = TextLayoutEngine.MeasureParagraph(p, gfx, innerWidth, currentPage, totalPages);
							cellLayout.Paragraphs.Add(pLayout);
							cellContentHeight += pLayout.TotalHeight;
						}
					}

					if (cellContentHeight > maxCellHeight) {
						maxCellHeight = cellContentHeight;
					}

					rowLayout.Cells.Add(cellLayout);
					colX += cellWidth;
					colIndex += span;
				}

				rowLayout.Height = Math.Max(maxCellHeight, 18.0); // Minimum row height 18pt
				foreach (var cellLayout in rowLayout.Cells) {
					cellLayout.Height = rowLayout.Height;
				}

				rowLayouts.Add(rowLayout);
			}

			return rowLayouts;
		}

		private static List<double> ResolveColumnWidths(TableModel table, double containerWidth) {
			List<double> colWidths = new List<double>();
			if (table.ColumnWidthsPt != null && table.ColumnWidthsPt.Count > 0) {
				colWidths.AddRange(table.ColumnWidthsPt);
			}

			if (colWidths.Count == 0 && table.Rows.Count > 0) {
				int maxCols = table.Rows.Max(r => r.Cells.Sum(c => Math.Max(1, c.GridSpan)));
				double equalWidth = containerWidth / Math.Max(1, maxCols);
				for (int i = 0; i < maxCols; i++) {
					colWidths.Add(equalWidth);
				}
			}

			// Normalize column widths if total exceeds containerWidth or if total is 0
			double totalWidth = colWidths.Sum();
			if (totalWidth > containerWidth || (totalWidth > 0 && Math.Abs(totalWidth - containerWidth) > 5.0)) {
				double scale = containerWidth / totalWidth;
				for (int i = 0; i < colWidths.Count; i++) {
					colWidths[i] *= scale;
				}
			}

			return colWidths;
		}

		private static CellPaddingModel MergePadding(CellPaddingModel cellPadding, CellPaddingModel defaultPadding) {
			return new CellPaddingModel {
				Top = cellPadding.Top > 0 ? cellPadding.Top : defaultPadding.Top,
				Bottom = cellPadding.Bottom > 0 ? cellPadding.Bottom : defaultPadding.Bottom,
				Left = cellPadding.Left > 0 ? cellPadding.Left : defaultPadding.Left,
				Right = cellPadding.Right > 0 ? cellPadding.Right : defaultPadding.Right
			};
		}

		/// <summary>
		/// Renders a single table row, including cell backgrounds, cell text/paragraphs, and cell borders onto the graphics canvas.
		/// </summary>
		/// <param name="rowLayout">The measured row layout model. Cannot be null.</param>
		/// <param name="table">The parent table model. Cannot be null.</param>
		/// <param name="gfx">PDFsharp graphics context. Cannot be null.</param>
		/// <param name="containerX">Left margin origin in points.</param>
		/// <param name="currentY">Top Y coordinate in points.</param>
		/// <param name="containerWidth">Available printable width in points.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="rowLayout"/>, <paramref name="table"/>, or <paramref name="gfx"/> is null.</exception>
		public static void RenderRow(TableRowLayout rowLayout, TableModel table, XGraphics gfx, double containerX, double currentY, double containerWidth = 0) {
			if (rowLayout == null) throw new ArgumentNullException(nameof(rowLayout));
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));
			if (containerWidth <= 0) {
				containerWidth = Math.Max(1.0, gfx.PageSize.Width - containerX * 2.0);
			}
			double alignOffset = 0;
			double tableWidth = rowLayout.Cells.Sum(c => c.Width);
			if (table.Alignment == ParagraphAlignment.Center) {
				alignOffset = Math.Max(0, (containerWidth - tableWidth) / 2.0);
			} else if (table.Alignment == ParagraphAlignment.Right) {
				alignOffset = Math.Max(0, containerWidth - tableWidth);
			}

			double rowX = containerX + alignOffset;

			foreach (var cellLayout in rowLayout.Cells) {
				double cellLeft = rowX + cellLayout.X;
				double cellTop = currentY;
				double cellWidth = cellLayout.Width;
				double cellHeight = rowLayout.Height;

				// 1. Draw cell background fill
				string? bgColorHex = cellLayout.Cell.BackgroundColorHex;
				if (!string.IsNullOrEmpty(bgColorHex) && !string.Equals(bgColorHex, "auto", StringComparison.OrdinalIgnoreCase)) {
					XColor bgColor = TextMeasurer.ParseColor(bgColorHex, XColors.White);
					gfx.DrawRectangle(new XSolidBrush(bgColor), cellLeft, cellTop, cellWidth, cellHeight);
				}

				// 2. Draw cell borders
				BordersModel borders = MergeBorders(cellLayout.Cell.Borders, table.Borders);
				DrawCellBorders(gfx, borders, cellLeft, cellTop, cellWidth, cellHeight);

				// 3. Render cell content (paragraphs)
				CellPaddingModel padding = MergePadding(cellLayout.Cell.Padding, table.DefaultCellPadding);
				double innerX = cellLeft + padding.Left;

				double contentHeight = cellLayout.Paragraphs.Sum(p => p.TotalHeight);
				double availCellHeight = cellHeight - padding.Top - padding.Bottom;
				double vOffset = 0;
				if (cellLayout.Cell.VerticalAlignment == CellVerticalAlignment.Center) {
					vOffset = Math.Max(0, (availCellHeight - contentHeight) / 2.0);
				} else if (cellLayout.Cell.VerticalAlignment == CellVerticalAlignment.Bottom) {
					vOffset = Math.Max(0, availCellHeight - contentHeight);
				}

				double innerY = cellTop + padding.Top + vOffset;

				foreach (var pLayout in cellLayout.Paragraphs) {
					TextLayoutEngine.RenderParagraph(pLayout, gfx, innerX, ref innerY);
				}
			}
		}

		private static BordersModel MergeBorders(BordersModel cellBorders, BordersModel tableBorders) {
			return new BordersModel {
				Top = cellBorders.Top.Style != BorderStyle.None ? cellBorders.Top : tableBorders.Top,
				Bottom = cellBorders.Bottom.Style != BorderStyle.None ? cellBorders.Bottom : tableBorders.Bottom,
				Left = cellBorders.Left.Style != BorderStyle.None ? cellBorders.Left : tableBorders.Left,
				Right = cellBorders.Right.Style != BorderStyle.None ? cellBorders.Right : tableBorders.Right
			};
		}

		private static void DrawCellBorders(XGraphics gfx, BordersModel borders, double left, double top, double width, double height) {
			DrawBorderSide(gfx, borders.Top, left, top, left + width, top);
			DrawBorderSide(gfx, borders.Bottom, left, top + height, left + width, top + height);
			DrawBorderSide(gfx, borders.Left, left, top, left, top + height);
			DrawBorderSide(gfx, borders.Right, left + width, top, left + width, top + height);
		}

		private static void DrawBorderSide(XGraphics gfx, BorderSideModel side, double x1, double y1, double x2, double y2) {
			if (side.Style == BorderStyle.None || side.WidthPt <= 0) return;

			XColor color = TextMeasurer.ParseColor(side.ColorHex, XColors.Black);
			XPen pen = new XPen(color, Math.Max(0.5, side.WidthPt));

			switch (side.Style) {
				case BorderStyle.Dotted:
					pen.DashStyle = XDashStyle.Dot;
					break;
				case BorderStyle.Dashed:
					pen.DashStyle = XDashStyle.Dash;
					break;
				case BorderStyle.Double:
					pen.DashStyle = XDashStyle.Solid;
					break;
				default:
					pen.DashStyle = XDashStyle.Solid;
					break;
			}

			gfx.DrawLine(pen, x1, y1, x2, y2);
		}
	}
}
