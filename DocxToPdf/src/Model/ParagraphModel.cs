using System.Collections.Generic;

namespace DocxToPdf.Model {
	public enum ParagraphAlignment {
		Left,
		Center,
		Right,
		Justify
	}

	public class ParagraphModel : IBlockElement {
		public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;
		public double SpacingBeforePt { get; set; }
		public double SpacingAfterPt { get; set; }
		public double LineHeightPt { get; set; } // 0 means default automatic line height
		public bool IsLineHeightMultiple { get; set; }

		public double LeftIndentPt { get; set; }
		public double RightIndentPt { get; set; }
		public double FirstLineIndentPt { get; set; }
		public double HangingIndentPt { get; set; }

		public ListFormatModel? ListFormat { get; set; }
		public List<RunModel> Runs { get; set; } = new List<RunModel>();
	}
}
