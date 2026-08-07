namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies dynamic field type embedded in run text (e.g. page numbers).
	/// </summary>
	public enum FieldType {
		/// <summary>Regular text run with no dynamic field.</summary>
		None,
		/// <summary>Dynamic current page number field (PAGE).</summary>
		PageNumber,
		/// <summary>Dynamic total pages count field (NUMPAGES).</summary>
		TotalPages
	}

	/// <summary>
	/// Model representing a contiguous run of inline text with consistent font, styling, and color formatting.
	/// </summary>
	public class RunModel {
		/// <summary>Gets or sets the text content of the run.</summary>
		public string Text { get; set; } = string.Empty;

		/// <summary>Gets or sets font family name (default "Arial").</summary>
		public string FontFamily { get; set; } = "Arial";

		/// <summary>Gets or sets font size in points (default 11.0pt).</summary>
		public double FontSizePt { get; set; } = 11.0;

		/// <summary>Gets or sets whether font is bold.</summary>
		public bool IsBold { get; set; }

		/// <summary>Gets or sets whether font is italic.</summary>
		public bool IsItalic { get; set; }

		/// <summary>Gets or sets whether text has underline decoration.</summary>
		public bool IsUnderline { get; set; }

		/// <summary>Gets or sets whether text has strikethrough decoration.</summary>
		public bool IsStrikeThrough { get; set; }

		/// <summary>
		/// Text color in HEX format (e.g. "#000000" or "#FF0000").
		/// </summary>
		public string TextColorHex { get; set; } = "#000000";

		/// <summary>
		/// Background highlight / shading color in HEX format (optional).
		/// </summary>
		public string? BackgroundColorHex { get; set; }

		/// <summary>Gets or sets dynamic field type if run represents a field code.</summary>
		public FieldType Field { get; set; } = FieldType.None;
	}
}
