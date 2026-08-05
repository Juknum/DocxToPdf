using System.Collections.Generic;

namespace DocxToPdf.Model {
	public class TableRowModel {
		public double HeightPt { get; set; }
		public bool IsHeader { get; set; }

		public List<TableCellModel> Cells { get; set; } = new List<TableCellModel>();
	}
}
