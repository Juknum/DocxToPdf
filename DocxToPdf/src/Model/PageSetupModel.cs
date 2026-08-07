namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies page orientation mode (Portrait or Landscape).
	/// </summary>
	public enum PageOrientation {
		/// <summary>Portrait orientation (vertical).</summary>
		Portrait,
		/// <summary>Landscape orientation (horizontal).</summary>
		Landscape
	}

	/// <summary>
	/// Model representing page margins and header/footer distances in points.
	/// </summary>
	public class PageMarginsModel {
		/// <summary>Gets or sets top margin in points (default 72pt = 1 inch).</summary>
		public double Top { get; set; } = 72.0;

		/// <summary>Gets or sets bottom margin in points (default 72pt = 1 inch).</summary>
		public double Bottom { get; set; } = 72.0;

		/// <summary>Gets or sets left margin in points (default 72pt = 1 inch).</summary>
		public double Left { get; set; } = 72.0;

		/// <summary>Gets or sets right margin in points (default 72pt = 1 inch).</summary>
		public double Right { get; set; } = 72.0;

		/// <summary>Gets or sets header distance from top edge in points (default 36pt = 0.5 inch).</summary>
		public double Header { get; set; } = 36.0;

		/// <summary>Gets or sets footer distance from bottom edge in points (default 36pt = 0.5 inch).</summary>
		public double Footer { get; set; } = 36.0;
	}

	/// <summary>
	/// Model representing section page setup properties (dimensions, orientation, margins).
	/// </summary>
	public class PageSetupModel {
		/// <summary>
		/// Page width in PDF points.
		/// Standard Letter = 612pt, A4 = 595.28pt.
		/// </summary>
		public double Width { get; set; } = 612.0;

		/// <summary>
		/// Page height in PDF points.
		/// Standard Letter = 792pt, A4 = 841.89pt.
		/// </summary>
		public double Height { get; set; } = 792.0;

		/// <summary>Gets or sets page orientation.</summary>
		public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

		/// <summary>Gets or sets page margin specifications.</summary>
		public PageMarginsModel Margins { get; set; } = new();

		/// <summary>Gets printable page width in points (Width - Left Margin - Right Margin).</summary>
		public double PrintableWidth => Width - Margins.Left - Margins.Right;

		/// <summary>Gets printable page height in points (Height - Top Margin - Bottom Margin).</summary>
		public double PrintableHeight => Height - Margins.Top - Margins.Bottom;
	}
}
