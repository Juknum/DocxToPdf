using System.Collections.Generic;

namespace DocxToPdf.Model {
	public enum VerticalMergeState {
		None,
		Restart,
		Continue
	}

	public class TableCellModel {
		public int GridSpan { get; set; } = 1;
		public VerticalMergeState VerticalMerge { get; set; } = VerticalMergeState.None;
		public double WidthPt { get; set; }

		public string? BackgroundColorHex { get; set; }
		public BordersModel Borders { get; set; } = new BordersModel();
		public CellPaddingModel Padding { get; set; } = new CellPaddingModel();

		public List<IBlockElement> Elements { get; set; } = new List<IBlockElement>();
	}
}
