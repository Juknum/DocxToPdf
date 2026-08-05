using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	public class TableCellLayout {
		public TableCellModel Cell { get; set; } = new TableCellModel();
		public double X { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public List<ParagraphLayout> Paragraphs { get; set; } = new List<ParagraphLayout>();
	}

	public class TableRowLayout {
		public TableRowModel Row { get; set; } = new TableRowModel();
		public List<TableCellLayout> Cells { get; set; } = new List<TableCellLayout>();
		public double Height { get; set; }
		public bool IsHeader => Row.IsHeader;
	}

	public static class TableRenderer {

		public static List<TableRowLayout> MeasureTable(TableModel table, XGraphics gfx, double containerWidth, int currentPage = 1, int totalPages = 1) {
			List<TableRowLayout> rowLayouts = new List<TableRowLayout>();

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

		public static void RenderRow(TableRowLayout rowLayout, TableModel table, XGraphics gfx, double containerX, double currentY) {
			double alignOffset = 0;
			if (table.Alignment == ParagraphAlignment.Center) {
				double tableWidth = rowLayout.Cells.Sum(c => c.Width);
				double availWidth = gfx.PageSize.Width; // Approximate container alignment
				alignOffset = Math.Max(0, (availWidth - tableWidth) / 2.0);
			} else if (table.Alignment == ParagraphAlignment.Right) {
				double tableWidth = rowLayout.Cells.Sum(c => c.Width);
				alignOffset = Math.Max(0, gfx.PageSize.Width - tableWidth);
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
				double innerY = cellTop + padding.Top;

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
