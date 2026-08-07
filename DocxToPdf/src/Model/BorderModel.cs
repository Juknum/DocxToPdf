namespace DocxToPdf.Model {
	/// <summary>
	/// Represents border line styles for table cells and borders.
	/// </summary>
	public enum BorderStyle {
		/// <summary>No border.</summary>
		None,
		/// <summary>Single solid line border.</summary>
		Single,
		/// <summary>Dotted line border.</summary>
		Dotted,
		/// <summary>Dashed line border.</summary>
		Dashed,
		/// <summary>Double line border.</summary>
		Double,
		/// <summary>Groove 3D border.</summary>
		Groove,
		/// <summary>Ridge 3D border.</summary>
		Ridge
	}

	/// <summary>
	/// Model representing a single side border specification (style, width in points, and color).
	/// </summary>
	public class BorderSideModel {
		/// <summary>Gets or sets the border line style.</summary>
		public BorderStyle Style { get; set; } = BorderStyle.None;

		/// <summary>Gets or sets the border line width in points.</summary>
		public double WidthPt { get; set; } = 0.5;

		/// <summary>Gets or sets the border line color in HEX format.</summary>
		public string ColorHex { get; set; } = "#000000";
	}

	/// <summary>
	/// Model representing 4-side borders (top, bottom, left, right) for tables and cells.
	/// </summary>
	public class BordersModel {
		/// <summary>Gets or sets top border side specification.</summary>
		public BorderSideModel Top { get; set; } = new();

		/// <summary>Gets or sets bottom border side specification.</summary>
		public BorderSideModel Bottom { get; set; } = new();

		/// <summary>Gets or sets left border side specification.</summary>
		public BorderSideModel Left { get; set; } = new();

		/// <summary>Gets or sets right border side specification.</summary>
		public BorderSideModel Right { get; set; } = new();
	}

	/// <summary>
	/// Model representing internal padding dimensions in points for table cells.
	/// </summary>
	public class CellPaddingModel {
		/// <summary>Gets or sets top padding in points.</summary>
		public double Top { get; set; } = 4.0;

		/// <summary>Gets or sets bottom padding in points.</summary>
		public double Bottom { get; set; } = 4.0;

		/// <summary>Gets or sets left padding in points.</summary>
		public double Left { get; set; } = 6.0;

		/// <summary>Gets or sets right padding in points.</summary>
		public double Right { get; set; } = 6.0;
	}
}
