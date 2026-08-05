using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	public class TableParser {
		private readonly StyleResolver _styleResolver;

		public TableParser(StyleResolver styleResolver) {
			_styleResolver = styleResolver;
		}

		public TableModel ParseTable(Table tbl, MediaResolver mediaResolver, ParagraphParser? paragraphParser = null) {
			TableModel tableModel = new TableModel();

			// 1. Table Properties (w:tblPr)
			TableProperties? tblPr = tbl.GetFirstChild<TableProperties>();
			if (tblPr != null) {
				if (tblPr.TableJustification?.Val != null) {
					tableModel.Alignment = MapTableJustification(tblPr.TableJustification.Val.Value);
				}

				if (tblPr.TableBorders != null) {
					tableModel.Borders = ParseTableBorders(tblPr.TableBorders);
				}

				if (tblPr.TableCellMarginDefault != null) {
					tableModel.DefaultCellPadding = ParseTableCellMarginDefault(tblPr.TableCellMarginDefault);
				}
			}

			// 2. Table Grid (w:tblGrid)
			TableGrid? tblGrid = tbl.GetFirstChild<TableGrid>();
			if (tblGrid != null) {
				foreach (GridColumn gridCol in tblGrid.Elements<GridColumn>()) {
					if (gridCol.Width != null && double.TryParse(gridCol.Width.Value, out double twips)) {
						tableModel.ColumnWidthsPt.Add(TwipConverter.TwipsToPoints(twips));
					}
				}
			}

			// 3. Table Rows (w:tr)
			foreach (TableRow tr in tbl.Elements<TableRow>()) {
				TableRowModel rowModel = new TableRowModel();
				TableRowProperties? trPr = tr.GetFirstChild<TableRowProperties>();
				if (trPr != null) {
					TableHeader? tblHeader = trPr.GetFirstChild<TableHeader>();
					if (tblHeader != null) {
						rowModel.IsHeader = tblHeader.Val?.Value != OnOffOnlyValues.Off;
					}

					TableRowHeight? trHeight = trPr.GetFirstChild<TableRowHeight>();
					if (trHeight?.Val?.Value != null) {
						rowModel.HeightPt = TwipConverter.TwipsToPoints(Convert.ToDouble(trHeight.Val.Value));
					}
				}

				// Cells (w:tc)
				foreach (TableCell tc in tr.Elements<TableCell>()) {
					TableCellModel cellModel = ParseTableCell(tc, tableModel.Borders, tableModel.DefaultCellPadding, mediaResolver, paragraphParser);
					rowModel.Cells.Add(cellModel);
				}

				tableModel.Rows.Add(rowModel);
			}

			return tableModel;
		}

		private TableCellModel ParseTableCell(TableCell tc, BordersModel defaultBorders, CellPaddingModel defaultPadding, MediaResolver mediaResolver, ParagraphParser? paragraphParser) {
			TableCellModel cellModel = new TableCellModel {
				Padding = new CellPaddingModel {
					Top = defaultPadding.Top,
					Bottom = defaultPadding.Bottom,
					Left = defaultPadding.Left,
					Right = defaultPadding.Right
				},
				Borders = new BordersModel {
					Top = new BorderSideModel { Style = defaultBorders.Top.Style, WidthPt = defaultBorders.Top.WidthPt, ColorHex = defaultBorders.Top.ColorHex },
					Bottom = new BorderSideModel { Style = defaultBorders.Bottom.Style, WidthPt = defaultBorders.Bottom.WidthPt, ColorHex = defaultBorders.Bottom.ColorHex },
					Left = new BorderSideModel { Style = defaultBorders.Left.Style, WidthPt = defaultBorders.Left.WidthPt, ColorHex = defaultBorders.Left.ColorHex },
					Right = new BorderSideModel { Style = defaultBorders.Right.Style, WidthPt = defaultBorders.Right.WidthPt, ColorHex = defaultBorders.Right.ColorHex }
				}
			};

			TableCellProperties? tcPr = tc.GetFirstChild<TableCellProperties>();
			if (tcPr != null) {
				// Column Span (w:gridSpan)
				GridSpan? gridSpan = tcPr.GetFirstChild<GridSpan>();
				if (gridSpan?.Val?.Value != null) {
					cellModel.GridSpan = gridSpan.Val.Value;
				}

				// Vertical Merge (w:vMerge)
				VerticalMerge? vMerge = tcPr.GetFirstChild<VerticalMerge>();
				if (vMerge != null) {
					if (vMerge.Val?.Value == MergedCellValues.Restart) {
						cellModel.VerticalMerge = VerticalMergeState.Restart;
					} else {
						cellModel.VerticalMerge = VerticalMergeState.Continue;
					}
				}

				// Cell Width (w:tcW)
				TableCellWidth? tcW = tcPr.GetFirstChild<TableCellWidth>();
				if (tcW?.Width?.Value != null && double.TryParse(tcW.Width.Value, out double twips)) {
					cellModel.WidthPt = TwipConverter.TwipsToPoints(twips);
				}

				// Cell Shading (w:shd)
				Shading? shd = tcPr.GetFirstChild<Shading>();
				if (shd?.Fill?.Value != null) {
					string bg = TwipConverter.NormalizeHexColor(shd.Fill.Value, string.Empty);
					if (!string.IsNullOrEmpty(bg)) {
						cellModel.BackgroundColorHex = bg;
					}
				}

				// Cell Borders (w:tcBorders)
				TableCellBorders? tcBorders = tcPr.GetFirstChild<TableCellBorders>();
				if (tcBorders != null) {
					ApplyCellBorders(cellModel.Borders, tcBorders);
				}

				// Cell Margins/Padding (w:tcMar)
				TableCellMargin? tcMar = tcPr.GetFirstChild<TableCellMargin>();
				if (tcMar != null) {
					ApplyCellMargin(cellModel.Padding, tcMar);
				}
			}

			// Parse Cell Elements (Paragraphs, nested Tables)
			if (paragraphParser != null) {
				foreach (var child in tc.ChildElements) {
					if (child is Paragraph p) {
						cellModel.Elements.Add(paragraphParser.ParseParagraph(p, mediaResolver));
					} else if (child is Table nestedTbl) {
						cellModel.Elements.Add(ParseTable(nestedTbl, mediaResolver, paragraphParser));
					}
				}
			}

			return cellModel;
		}

		private ParagraphAlignment MapTableJustification(TableRowAlignmentValues val) {
			if (val == TableRowAlignmentValues.Center) return ParagraphAlignment.Center;
			if (val == TableRowAlignmentValues.Right) return ParagraphAlignment.Right;
			return ParagraphAlignment.Left;
		}

		private BordersModel ParseTableBorders(TableBorders tblBorders) {
			BordersModel borders = new BordersModel();
			if (tblBorders.TopBorder != null) borders.Top = MapBorderSide(tblBorders.TopBorder);
			if (tblBorders.BottomBorder != null) borders.Bottom = MapBorderSide(tblBorders.BottomBorder);
			if (tblBorders.LeftBorder != null) borders.Left = MapBorderSide(tblBorders.LeftBorder);
			if (tblBorders.RightBorder != null) borders.Right = MapBorderSide(tblBorders.RightBorder);
			return borders;
		}

		private CellPaddingModel ParseTableCellMarginDefault(TableCellMarginDefault tblCellMar) {
			CellPaddingModel padding = new CellPaddingModel();
			if (tblCellMar.TopMargin?.Width?.Value != null)
				padding.Top = TwipConverter.TwipsToPoints(Convert.ToDouble(tblCellMar.TopMargin.Width.Value));
			if (tblCellMar.BottomMargin?.Width?.Value != null)
				padding.Bottom = TwipConverter.TwipsToPoints(Convert.ToDouble(tblCellMar.BottomMargin.Width.Value));
			if (tblCellMar.TableCellLeftMargin?.Width?.Value != null)
				padding.Left = TwipConverter.TwipsToPoints(Convert.ToDouble(tblCellMar.TableCellLeftMargin.Width.Value));
			if (tblCellMar.TableCellRightMargin?.Width?.Value != null)
				padding.Right = TwipConverter.TwipsToPoints(Convert.ToDouble(tblCellMar.TableCellRightMargin.Width.Value));
			return padding;
		}

		private void ApplyCellBorders(BordersModel target, TableCellBorders tcBorders) {
			if (tcBorders.TopBorder != null) target.Top = MapBorderSide(tcBorders.TopBorder);
			if (tcBorders.BottomBorder != null) target.Bottom = MapBorderSide(tcBorders.BottomBorder);
			if (tcBorders.LeftBorder != null) target.Left = MapBorderSide(tcBorders.LeftBorder);
			if (tcBorders.RightBorder != null) target.Right = MapBorderSide(tcBorders.RightBorder);
		}

		private void ApplyCellMargin(CellPaddingModel target, TableCellMargin tcMar) {
			TopMargin? top = tcMar.GetFirstChild<TopMargin>();
			if (top?.Width?.Value != null)
				target.Top = TwipConverter.TwipsToPoints(Convert.ToDouble(top.Width.Value));

			BottomMargin? bottom = tcMar.GetFirstChild<BottomMargin>();
			if (bottom?.Width?.Value != null)
				target.Bottom = TwipConverter.TwipsToPoints(Convert.ToDouble(bottom.Width.Value));

			LeftMargin? left = tcMar.GetFirstChild<LeftMargin>();
			if (left?.Width?.Value != null)
				target.Left = TwipConverter.TwipsToPoints(Convert.ToDouble(left.Width.Value));

			RightMargin? right = tcMar.GetFirstChild<RightMargin>();
			if (right?.Width?.Value != null)
				target.Right = TwipConverter.TwipsToPoints(Convert.ToDouble(right.Width.Value));
		}

		private BorderSideModel MapBorderSide(BorderType borderType) {
			if (borderType.Val?.Value == BorderValues.None || borderType.Val?.Value == BorderValues.Nil) {
				return new BorderSideModel { Style = BorderStyle.None };
			}

			double widthPt = 0.5;
			if (borderType.Size?.Value != null) {
				widthPt = TwipConverter.EighthPointsToPoints(borderType.Size.Value);
			}

			string colorHex = TwipConverter.NormalizeHexColor(borderType.Color?.Value, "#000000");

			return new BorderSideModel {
				Style = BorderStyle.Single,
				WidthPt = widthPt,
				ColorHex = colorHex
			};
		}
	}
}
