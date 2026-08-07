using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Model representing a single table row containing cells, height specifications, and header flag.
	/// </summary>
	public class TableRowModel {
		/// <summary>Gets or sets row height in points (0 indicates automatic height).</summary>
		public double HeightPt { get; set; }

		/// <summary>Gets or sets whether row is a header row repeated at top of pages.</summary>
		public bool IsHeader { get; set; }

		/// <summary>Gets or sets table cells collection within this row.</summary>
		public List<TableCellModel> Cells { get; set; } = [];
	}
}
