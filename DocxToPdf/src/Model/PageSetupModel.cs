namespace DocxToPdf.Model {
	public enum PageOrientation {
		Portrait,
		Landscape
	}

	public class PageMarginsModel {
		public double Top { get; set; } = 72.0;    // 1 inch default
		public double Bottom { get; set; } = 72.0;
		public double Left { get; set; } = 72.0;
		public double Right { get; set; } = 72.0;
		public double Header { get; set; } = 36.0; // 0.5 inch default
		public double Footer { get; set; } = 36.0;
	}

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

		public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
		public PageMarginsModel Margins { get; set; } = new PageMarginsModel();

		public double PrintableWidth => Width - Margins.Left - Margins.Right;
		public double PrintableHeight => Height - Margins.Top - Margins.Bottom;
	}
}
