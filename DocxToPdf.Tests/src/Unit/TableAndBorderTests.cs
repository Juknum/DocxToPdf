using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class TableAndBorderTests {
		[Fact]
		public void TestFullTablePropertiesAndCellBorders() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					var table = new Table(
						new TableProperties(
							new TableJustification { Val = TableRowAlignmentValues.Right },
							new TableBorders(
								new TopBorder { Val = BorderValues.Single, Size = 16, Color = "FF0000" },
								new BottomBorder { Val = BorderValues.None },
								new LeftBorder { Val = BorderValues.Single, Size = 8, Color = "00FF00" },
								new RightBorder { Val = BorderValues.Nil }
							),
							new TableCellMarginDefault(
								new TopMargin { Width = "144" }, // 7.2pt
								new BottomMargin { Width = "144" },
								new TableCellLeftMargin { Width = 288 }, // 14.4pt
								new TableCellRightMargin { Width = 288 }
							)
						),
						new TableGrid(
							new GridColumn { Width = "1440" },
							new GridColumn { Width = "2880" }
						),
						// Row 1 (Header disabled explicitly)
						new TableRow(
							new TableRowProperties(
								new TableHeader { Val = OnOffOnlyValues.Off },
								new TableRowHeight { Val = 720 } // 36pt
							),
							new TableCell(
								new TableCellProperties(
									new TableCellWidth { Width = "1440" },
									new VerticalMerge { Val = MergedCellValues.Restart },
									new TableCellBorders(
										new TopBorder { Val = BorderValues.Single, Size = 24, Color = "0000FF" },
										new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "FFFF00" },
										new LeftBorder { Val = BorderValues.Single, Size = 8, Color = "000000" },
										new RightBorder { Val = BorderValues.Single, Size = 8, Color = "000000" }
									),
									new TableCellMargin(
										new TopMargin { Width = "72" },
										new BottomMargin { Width = "72" },
										new LeftMargin { Width = "144" },
										new RightMargin { Width = "144" }
									)
								),
								new Paragraph(new Run(new Text("Cell 1")))
							),
							new TableCell(
								new TableCellProperties(
									new TableCellWidth { Width = "2880" },
									new Shading { Fill = "CCCCCC" }
								),
								// Nested table
								new Table(
									new TableRow(
										new TableCell(new Paragraph(new Run(new Text("Nested Cell"))))
									)
								)
							)
						),
						// Row 2
						new TableRow(
							new TableCell(
								new TableCellProperties(
									new VerticalMerge { Val = MergedCellValues.Continue }
								),
								new Paragraph(new Run(new Text("Cell 1 Continue")))
							),
							new TableCell(
								new Paragraph(new Run(new Text("Cell 2 Continuation")))
							)
						)
					);

					mainPart.Document = new Document(new Body(table));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					TableModel parsedTable = Assert.IsType<TableModel>(model.Sections[0].Elements[0]);

					Assert.Equal(ParagraphAlignment.Right, parsedTable.Alignment);
					Assert.Equal(2, parsedTable.ColumnWidthsPt.Count);
					Assert.Equal(72.0, parsedTable.ColumnWidthsPt[0]);
					Assert.Equal(144.0, parsedTable.ColumnWidthsPt[1]);

					// Borders
					Assert.Equal(BorderStyle.Single, parsedTable.Borders.Top.Style);
					Assert.Equal(2.0, parsedTable.Borders.Top.WidthPt);
					Assert.Equal("#FF0000", parsedTable.Borders.Top.ColorHex);
					Assert.Equal(BorderStyle.None, parsedTable.Borders.Bottom.Style);
					Assert.Equal(BorderStyle.None, parsedTable.Borders.Right.Style);

					// Default Cell Padding
					Assert.Equal(7.2, parsedTable.DefaultCellPadding.Top);
					Assert.Equal(14.4, parsedTable.DefaultCellPadding.Left);

					// Rows
					Assert.Equal(2, parsedTable.Rows.Count);
					Assert.False(parsedTable.Rows[0].IsHeader);
					Assert.Equal(36.0, parsedTable.Rows[0].HeightPt);

					// Cell 1
					TableCellModel cell1 = parsedTable.Rows[0].Cells[0];
					Assert.Equal(72.0, cell1.WidthPt);
					Assert.Equal(VerticalMergeState.Restart, cell1.VerticalMerge);
					Assert.Equal(3.0, cell1.Borders.Top.WidthPt);
					Assert.Equal("#0000FF", cell1.Borders.Top.ColorHex);
					Assert.Equal(3.6, cell1.Padding.Top);

					// Cell 2 (with nested table)
					TableCellModel cell2 = parsedTable.Rows[0].Cells[1];
					Assert.Equal("#CCCCCC", cell2.BackgroundColorHex);
					Assert.Single(cell2.Elements);
					Assert.IsType<TableModel>(cell2.Elements[0]);

					// Row 2 Cell 1
					TableCellModel row2cell1 = parsedTable.Rows[1].Cells[0];
					Assert.Equal(VerticalMergeState.Continue, row2cell1.VerticalMerge);
				}
			}
		}

		[Fact]
		public void TestTableJustificationCenter() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					mainPart.Document = new Document(new Body(
						new Table(
							new TableProperties(
								new TableJustification { Val = TableRowAlignmentValues.Center }
							),
							new TableRow(new TableCell(new Paragraph(new Run(new Text("Centered")))))
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);
					TableModel table = Assert.IsType<TableModel>(model.Sections[0].Elements[0]);
					Assert.Equal(ParagraphAlignment.Center, table.Alignment);
				}
			}
		}
	}
}
