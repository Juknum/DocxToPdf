using Xunit;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using DocxToPdf.Model;
using DocxToPdf.Rendering;
using DocxToPdf.Fonts;

namespace DocxToPdf.Tests {
	public class TableRenderingTests {

		static TableRenderingTests() {
			CrossPlatformFontResolver.Register();
		}

		[Fact]
		public void TestTableColumnWidthResolutionAndCellMeasurement() {
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			TableModel table = new TableModel {
				ColumnWidthsPt = { 100, 200 }
			};

			TableRowModel row = new TableRowModel { HeightPt = 30 };
			TableCellModel cell1 = new TableCellModel {
				BackgroundColorHex = "#FF0000"
			};
			cell1.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Cell 1", FontSizePt = 10 } } });

			TableCellModel cell2 = new TableCellModel();
			cell2.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Cell 2 Content", FontSizePt = 10 } } });

			row.Cells.Add(cell1);
			row.Cells.Add(cell2);
			table.Rows.Add(row);

			var rowLayouts = TableRenderer.MeasureTable(table, gfx, containerWidth: 300);

			Assert.Single(rowLayouts);
			Assert.Equal(2, rowLayouts[0].Cells.Count);
			Assert.Equal(100, rowLayouts[0].Cells[0].Width);
			Assert.Equal(200, rowLayouts[0].Cells[1].Width);
			Assert.True(rowLayouts[0].Height >= 30);
		}

		[Fact]
		public void TestTableCellSpanning() {
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			TableModel table = new TableModel {
				ColumnWidthsPt = { 100, 100, 100 }
			};

			TableRowModel row = new TableRowModel();
			TableCellModel spanCell = new TableCellModel { GridSpan = 2 };
			spanCell.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Spanning 2 Cols" } } });

			TableCellModel singleCell = new TableCellModel { GridSpan = 1 };
			singleCell.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Single Col" } } });

			row.Cells.Add(spanCell);
			row.Cells.Add(singleCell);
			table.Rows.Add(row);

			var rowLayouts = TableRenderer.MeasureTable(table, gfx, containerWidth: 300);

			Assert.Equal(200, rowLayouts[0].Cells[0].Width);
			Assert.Equal(100, rowLayouts[0].Cells[1].Width);
		}
	}
}
