namespace DocxToPdf.Constants {
	/// <summary>
	/// Centralized constant values for font family names.
	/// </summary>
	public static class FontConstants {
		/// <summary>Default font family name used when unspecified ("Arial").</summary>
		public const string DefaultFontFamily = "Arial";

		/// <summary>Calibri font family name ("Calibri").</summary>
		public const string Calibri = "Calibri";

		/// <summary>Times New Roman font family name ("Times New Roman").</summary>
		public const string TimesNewRoman = "Times New Roman";
	}

	/// <summary>
	/// Centralized constant values for HEX colors and color keyword strings.
	/// </summary>
	public static class ColorConstants {
		/// <summary>Default black color HEX code ("#000000").</summary>
		public const string DefaultBlackHex = "#000000";

		/// <summary>Default white color HEX code ("#FFFFFF").</summary>
		public const string DefaultWhiteHex = "#FFFFFF";

		/// <summary>Automatic color string literal ("auto").</summary>
		public const string Auto = "auto";
	}

	/// <summary>
	/// Centralized constant values for media MIME content types.
	/// </summary>
	public static class MediaConstants {
		/// <summary>PNG image content type ("image/png").</summary>
		public const string PngContentType = "image/png";

		/// <summary>JPEG image content type ("image/jpeg").</summary>
		public const string JpegContentType = "image/jpeg";

		/// <summary>EMF vector image content type ("image/x-emf").</summary>
		public const string EmfContentType = "image/x-emf";
	}

	/// <summary>
	/// Centralized OpenXML string literals and attribute constants.
	/// </summary>
	public static class OpenXmlConstants {
		/// <summary>OpenXML bullet format keyword ("bullet").</summary>
		public const string Bullet = "bullet";

		/// <summary>OpenXML none format keyword ("none").</summary>
		public const string None = "none";

		/// <summary>OpenXML margin keyword ("margin").</summary>
		public const string Margin = "margin";

		/// <summary>OpenXML page keyword ("page").</summary>
		public const string Page = "page";

		/// <summary>OpenXML paragraph keyword ("paragraph").</summary>
		public const string Paragraph = "paragraph";

		/// <summary>OpenXML line keyword ("line").</summary>
		public const string Line = "line";

		/// <summary>OpenXML column keyword ("column").</summary>
		public const string Column = "column";

		/// <summary>OpenXML character keyword ("character").</summary>
		public const string Character = "character";
	}
}
