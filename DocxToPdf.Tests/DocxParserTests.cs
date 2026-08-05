using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class DocxParserTests {

		[Fact]
		public void TestParseSimpleParagraphDocument() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					mainPart.Document = new Document(new Body(
						new Paragraph(
							new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
							new Run(
								new RunProperties(new Bold(), new FontSize { Val = "24" }, new Color { Val = "FF0000" }),
								new Text("Hello World")
							)
						),
						new SectionProperties(
							new PageSize { Width = 12240, Height = 15840 }, // Letter 612x792 pt
							new PageMargin { Top = 1440, Bottom = 1440, Left = 1440, Right = 1440 } // 72pt margins
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					Assert.Single(model.Sections);
					SectionModel section = model.Sections[0];
					Assert.Equal(612.0, section.PageSetup.Width);
					Assert.Equal(792.0, section.PageSetup.Height);
					Assert.Equal(72.0, section.PageSetup.Margins.Top);

					Assert.Single(section.Elements);
					ParagraphModel paragraph = Assert.IsType<ParagraphModel>(section.Elements[0]);
					Assert.Equal(ParagraphAlignment.Center, paragraph.Alignment);

					Assert.Single(paragraph.Runs);
					RunModel run = paragraph.Runs[0];
					Assert.Equal("Hello World", run.Text);
					Assert.True(run.IsBold);
					Assert.Equal(12.0, run.FontSizePt);
					Assert.Equal("#FF0000", run.TextColorHex);
				}
			}
		}

		[Fact]
		public void TestParseTableWithSpansAndBorders() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					mainPart.Document = new Document(new Body(
						new Table(
							new TableGrid(
								new GridColumn { Width = "2880" },
								new GridColumn { Width = "2880" }
							),
							new TableRow(
								new TableRowProperties(new TableHeader()),
								new TableCell(
									new TableCellProperties(new GridSpan { Val = 2 }, new Shading { Fill = "00FF00" }),
									new Paragraph(new Run(new Text("Header Span")))
								)
							),
							new TableRow(
								new TableCell(new Paragraph(new Run(new Text("Cell 1")))),
								new TableCell(new Paragraph(new Run(new Text("Cell 2"))))
							)
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					Assert.Single(model.Sections);
					TableModel table = Assert.IsType<TableModel>(model.Sections[0].Elements[0]);

					Assert.Equal(2, table.ColumnWidthsPt.Count);
					Assert.Equal(144.0, table.ColumnWidthsPt[0]);

					Assert.Equal(2, table.Rows.Count);
					Assert.True(table.Rows[0].IsHeader);
					Assert.Equal(2, table.Rows[0].Cells[0].GridSpan);
					Assert.Equal("#00FF00", table.Rows[0].Cells[0].BackgroundColorHex);

					Assert.Equal(2, table.Rows[1].Cells.Count);
				}
			}
		}
	}
}
