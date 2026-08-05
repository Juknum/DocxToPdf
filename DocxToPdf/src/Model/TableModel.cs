using System.Collections.Generic;

namespace DocxToPdf.Model {
	public class TableModel : IBlockElement {
		public List<double> ColumnWidthsPt { get; set; } = new List<double>();
		public BordersModel Borders { get; set; } = new BordersModel();
		public CellPaddingModel DefaultCellPadding { get; set; } = new CellPaddingModel();
		public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;

		public List<TableRowModel> Rows { get; set; } = new List<TableRowModel>();
	}
}
