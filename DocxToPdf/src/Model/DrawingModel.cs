using System.IO;

namespace DocxToPdf.Model {
	public enum DrawingPlacement {
		Inline,
		Floating
	}

	public class DrawingModel : IBlockElement {
		public string RelationshipId { get; set; } = string.Empty;
		public byte[] ImageData { get; set; } = System.Array.Empty<byte>();
		public string ContentType { get; set; } = "image/png";

		public double WidthPt { get; set; }
		public double HeightPt { get; set; }

		public DrawingPlacement Placement { get; set; } = DrawingPlacement.Inline;
		public double OffsetXPt { get; set; }
		public double OffsetYPt { get; set; }
	}
}
