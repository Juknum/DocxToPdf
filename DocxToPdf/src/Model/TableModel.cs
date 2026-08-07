using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Model representing a block-level table containing column widths, borders, default cell margins, and row collections.
	/// </summary>
	public class TableModel : IBlockElement {
		/// <summary>Gets or sets grid column widths in points.</summary>
		public List<double> ColumnWidthsPt { get; set; } = [];

		/// <summary>Gets or sets default table-level 4-side borders.</summary>
		public BordersModel Borders { get; set; } = new();

		/// <summary>Gets or sets default cell padding for cells within this table.</summary>
		public CellPaddingModel DefaultCellPadding { get; set; } = new();

		/// <summary>Gets or sets default background shading color in HEX format.</summary>
		public string? DefaultBackgroundColorHex { get; set; }

		/// <summary>Gets or sets table alignment (Left, Center, Right).</summary>
		public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;

		/// <summary>Gets or sets table rows collection.</summary>
		public List<TableRowModel> Rows { get; set; } = [];
	}
}
