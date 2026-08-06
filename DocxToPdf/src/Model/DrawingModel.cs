using System.IO;

namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies the placement style of a drawing element within a document.
	/// </summary>
	public enum DrawingPlacement {
		Inline,
		Floating
	}

	/// <summary>
	/// Represents an extracted OpenXML drawing or vector shape model with geometry, styling, positioning, and depth properties.
	/// </summary>
	public class DrawingModel : IBlockElement {
		/// <summary>Gets or sets the OpenXML relationship ID for image binary parts.</summary>
		public string RelationshipId { get; set; } = string.Empty;

		/// <summary>Gets or sets raw image stream byte array.</summary>
		public byte[] ImageData { get; set; } = System.Array.Empty<byte>();

		/// <summary>Gets or sets the image MIME content type (e.g., image/png, image/x-emf).</summary>
		public string ContentType { get; set; } = "image/png";

		/// <summary>Gets or sets the shape width in points (1 pt = 1/72 in).</summary>
		public double WidthPt { get; set; }

		/// <summary>Gets or sets the shape height in points (1 pt = 1/72 in).</summary>
		public double HeightPt { get; set; }

		/// <summary>Gets or sets whether the drawing is inline or floating.</summary>
		public DrawingPlacement Placement { get; set; } = DrawingPlacement.Inline;

		/// <summary>Gets or sets the horizontal offset in points relative to the reference frame.</summary>
		public double OffsetXPt { get; set; }

		/// <summary>Gets or sets the vertical offset in points relative to the reference frame.</summary>
		public double OffsetYPt { get; set; }

		/// <summary>Gets or sets horizontal alignment string (left, center, right).</summary>
		public string? AlignH { get; set; }

		/// <summary>Gets or sets vertical alignment string (top, center, bottom).</summary>
		public string? AlignV { get; set; }

		/// <summary>Gets or sets whether the drawing is in the background layer behind text.</summary>
		public bool BehindDoc { get; set; }

		/// <summary>Gets or sets vertical reference frame (margin, page, paragraph, line).</summary>
		public string? VerticalRelativeFrom { get; set; }

		/// <summary>Gets or sets horizontal reference frame (margin, page, column, character).</summary>
		public string? HorizontalRelativeFrom { get; set; }

		/// <summary>Gets or sets shape background fill hex color string.</summary>
		public string? FillColorHex { get; set; }

		/// <summary>Gets or sets shape outline border hex color string.</summary>
		public string? BorderColorHex { get; set; }

		/// <summary>Gets or sets the Z-Index depth relative height for layer ordering.</summary>
		public long ZIndex { get; set; }

		/// <summary>Gets or sets nested paragraph models contained within shape textboxes.</summary>
		public System.Collections.Generic.List<ParagraphModel> TextboxParagraphs { get; set; } = new System.Collections.Generic.List<ParagraphModel>();
	}
}
