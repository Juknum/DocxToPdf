using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies vertical cell merging state (None, Restart, or Continue).
	/// </summary>
	public enum VerticalMergeState {
		/// <summary>No vertical merging.</summary>
		None,
		/// <summary>Starts a new vertical merge group.</summary>
		Restart,
		/// <summary>Continues an existing vertical merge group from the cell above.</summary>
		Continue
	}

	/// <summary>
	/// Specifies vertical alignment of content within a table cell (Top, Center, or Bottom).
	/// </summary>
	public enum CellVerticalAlignment {
		/// <summary>Align content to top of cell.</summary>
		Top,
		/// <summary>Center content vertically within cell.</summary>
		Center,
		/// <summary>Align content to bottom of cell.</summary>
		Bottom
	}

	/// <summary>
	/// Model representing a single table cell with grid spanning, vertical merge state, alignment, borders, padding, and block elements.
	/// </summary>
	public class TableCellModel {
		/// <summary>Gets or sets horizontal column span count (default 1).</summary>
		public int GridSpan { get; set; } = 1;

		/// <summary>Gets or sets vertical merge state.</summary>
		public VerticalMergeState VerticalMerge { get; set; } = VerticalMergeState.None;

		/// <summary>Gets or sets vertical content alignment.</summary>
		public CellVerticalAlignment VerticalAlignment { get; set; } = CellVerticalAlignment.Top;

		/// <summary>Gets or sets cell width in points.</summary>
		public double WidthPt { get; set; }

		/// <summary>Gets or sets cell background shading color in HEX format.</summary>
		public string? BackgroundColorHex { get; set; }

		/// <summary>Gets or sets 4-side border specifications for the cell.</summary>
		public BordersModel Borders { get; set; } = new();

		/// <summary>Gets or sets internal cell padding dimensions.</summary>
		public CellPaddingModel Padding { get; set; } = new();

		/// <summary>Gets or sets block-level elements (paragraphs, nested tables) contained in this cell.</summary>
		public List<IBlockElement> Elements { get; set; } = [];
	}
}
