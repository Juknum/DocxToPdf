namespace DocxToPdf.Model {
	public enum FieldType {
		None,
		PageNumber,
		TotalPages
	}

	public class RunModel {
		public string Text { get; set; } = string.Empty;
		public string FontFamily { get; set; } = "Arial";
		public double FontSizePt { get; set; } = 11.0;
		public bool IsBold { get; set; }
		public bool IsItalic { get; set; }
		public bool IsUnderline { get; set; }
		public bool IsStrikeThrough { get; set; }

		/// <summary>
		/// Text color in HEX format (e.g. "#000000" or "#FF0000").
		/// </summary>
		public string TextColorHex { get; set; } = "#000000";

		/// <summary>
		/// Background highlight / shading color in HEX format (optional).
		/// </summary>
		public string? BackgroundColorHex { get; set; }

		public FieldType Field { get; set; } = FieldType.None;
	}
}
