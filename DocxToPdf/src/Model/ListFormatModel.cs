namespace DocxToPdf.Model {
	public enum ListType {
		Bullet,
		Numbered
	}

	public class ListFormatModel {
		public int NumberingId { get; set; }
		public int Level { get; set; }
		public ListType Type { get; set; } = ListType.Bullet;

		/// <summary>
		/// Bullet symbol (e.g. "•", "▪") or formatted index text (e.g. "1.", "A.", "I.").
		/// </summary>
		public string MarkerText { get; set; } = "•";

		public double LeftIndentPt { get; set; }
		public double HangingIndentPt { get; set; }
	}
}
