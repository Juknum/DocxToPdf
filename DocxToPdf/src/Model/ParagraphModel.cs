using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies text alignment for a paragraph.
	/// </summary>
	public enum ParagraphAlignment {
		/// <summary>Left alignment.</summary>
		Left,
		/// <summary>Center alignment.</summary>
		Center,
		/// <summary>Right alignment.</summary>
		Right,
		/// <summary>Justified alignment.</summary>
		Justify
	}

	/// <summary>
	/// Model representing a block-level paragraph with alignment, spacing, indents, list formatting, and text runs.
	/// </summary>
	public class ParagraphModel : IBlockElement {
		/// <summary>Gets or sets paragraph text alignment.</summary>
		public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;

		/// <summary>Gets or sets spacing before paragraph in points.</summary>
		public double SpacingBeforePt { get; set; }

		/// <summary>Gets or sets spacing after paragraph in points.</summary>
		public double SpacingAfterPt { get; set; }

		/// <summary>Gets or sets line height in points (0 indicates default automatic calculation).</summary>
		public double LineHeightPt { get; set; }

		/// <summary>Gets or sets whether line height is specified as a multiple of line pitch.</summary>
		public bool IsLineHeightMultiple { get; set; }

		/// <summary>Gets or sets left margin indentation in points.</summary>
		public double LeftIndentPt { get; set; }

		/// <summary>Gets or sets right margin indentation in points.</summary>
		public double RightIndentPt { get; set; }

		/// <summary>Gets or sets first line indentation in points.</summary>
		public double FirstLineIndentPt { get; set; }

		/// <summary>Gets or sets hanging indentation in points.</summary>
		public double HangingIndentPt { get; set; }

		/// <summary>Gets or sets list formatting details if paragraph is a list item.</summary>
		public ListFormatModel? ListFormat { get; set; }

		/// <summary>Gets or sets background shading color in HEX format.</summary>
		public string? BackgroundColorHex { get; set; }

		/// <summary>Gets or sets the list of text and inline runs in this paragraph.</summary>
		public List<RunModel> Runs { get; set; } = [];

		/// <summary>Gets or sets whether a page break precedes this paragraph.</summary>
		public bool HasPageBreak { get; set; }
	}
}
