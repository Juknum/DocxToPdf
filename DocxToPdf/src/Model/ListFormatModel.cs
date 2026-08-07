namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies list item type (bullet or numbered).
	/// </summary>
	public enum ListType {
		/// <summary>Bullet point list item.</summary>
		Bullet,
		/// <summary>Numbered/ordered list item.</summary>
		Numbered
	}

	/// <summary>
	/// Model representing list formatting (bullets, numbers, indentations) applied to a paragraph.
	/// </summary>
	public class ListFormatModel {
		/// <summary>Gets or sets the numbering definition ID.</summary>
		public int NumberingId { get; set; }

		/// <summary>Gets or sets the 0-indexed list indentation level.</summary>
		public int Level { get; set; }

		/// <summary>Gets or sets the list item type.</summary>
		public ListType Type { get; set; } = ListType.Bullet;

		/// <summary>
		/// Bullet symbol (e.g. "•", "▪") or formatted index text (e.g. "1.", "A.", "I.").
		/// </summary>
		public string MarkerText { get; set; } = "•";

		/// <summary>Gets or sets the left indentation in points.</summary>
		public double LeftIndentPt { get; set; }

		/// <summary>Gets or sets the hanging indentation in points.</summary>
		public double HangingIndentPt { get; set; }
	}
}
