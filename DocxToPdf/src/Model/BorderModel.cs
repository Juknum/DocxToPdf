namespace DocxToPdf.Model {
	public enum BorderStyle {
		None,
		Single,
		Dotted,
		Dashed,
		Double,
		Groove,
		Ridge
	}

	public class BorderSideModel {
		public BorderStyle Style { get; set; } = BorderStyle.None;
		public double WidthPt { get; set; } = 0.5;
		public string ColorHex { get; set; } = "#000000";
	}

	public class BordersModel {
		public BorderSideModel Top { get; set; } = new BorderSideModel();
		public BorderSideModel Bottom { get; set; } = new BorderSideModel();
		public BorderSideModel Left { get; set; } = new BorderSideModel();
		public BorderSideModel Right { get; set; } = new BorderSideModel();
	}

	public class CellPaddingModel {
		public double Top { get; set; } = 4.0;
		public double Bottom { get; set; } = 4.0;
		public double Left { get; set; } = 6.0;
		public double Right { get; set; } = 6.0;
	}
}
