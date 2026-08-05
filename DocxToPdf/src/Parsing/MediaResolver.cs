using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	public class MediaResolver {
		private readonly OpenXmlPartContainer _partContainer;

		public MediaResolver(OpenXmlPartContainer partContainer) {
			_partContainer = partContainer;
		}

		public DrawingModel? ExtractDrawing(Drawing drawing) {
			if (drawing == null) return null;

			Wp.Inline? inline = drawing.GetFirstChild<Wp.Inline>();
			Wp.Anchor? anchor = drawing.GetFirstChild<Wp.Anchor>();

			long cx = 0;
			long cy = 0;
			DrawingPlacement placement = DrawingPlacement.Inline;
			string? relationshipId = null;

			if (inline != null) {
				placement = DrawingPlacement.Inline;
				if (inline.Extent != null) {
					cx = inline.Extent.Cx?.Value ?? 0;
					cy = inline.Extent.Cy?.Value ?? 0;
				}
				relationshipId = FindBlipRelationshipId(inline);
			} else if (anchor != null) {
				placement = DrawingPlacement.Floating;
				if (anchor.Extent != null) {
					cx = anchor.Extent.Cx?.Value ?? 0;
					cy = anchor.Extent.Cy?.Value ?? 0;
				}
				relationshipId = FindBlipRelationshipId(anchor);
			} else {
				relationshipId = FindBlipRelationshipId(drawing);
			}

			if (string.IsNullOrEmpty(relationshipId)) {
				return null;
			}

			return ExtractImageByRelationshipId(relationshipId!, placement, cx, cy);
		}

		public DrawingModel? ExtractPict(Picture pict) {
			if (pict == null) return null;

			string? relationshipId = FindBlipRelationshipId(pict);
			if (string.IsNullOrEmpty(relationshipId)) {
				return null;
			}

			return ExtractImageByRelationshipId(relationshipId!, DrawingPlacement.Inline, 0, 0);
		}

		public DrawingModel? ExtractImageByRelationshipId(string relationshipId, DrawingPlacement placement, long cx, long cy) {
			if (string.IsNullOrEmpty(relationshipId)) return null;

			ImagePart? imagePart = null;
			try {
				imagePart = _partContainer.GetPartById(relationshipId) as ImagePart;
			} catch {
				return null;
			}

			if (imagePart == null) {
				return null;
			}

			byte[] imageData;
			using (Stream stream = imagePart.GetStream())
			using (MemoryStream ms = new MemoryStream()) {
				stream.CopyTo(ms);
				imageData = ms.ToArray();
			}

			return new DrawingModel {
				RelationshipId = relationshipId,
				ImageData = imageData,
				ContentType = imagePart.ContentType ?? "image/png",
				WidthPt = TwipConverter.EmusToPoints(cx),
				HeightPt = TwipConverter.EmusToPoints(cy),
				Placement = placement
			};
		}

		private string? FindBlipRelationshipId(OpenXmlElement element) {
			foreach (var desc in element.Descendants<A.Blip>()) {
				if (desc.Embed?.Value != null && !string.IsNullOrEmpty(desc.Embed.Value)) {
					return desc.Embed.Value;
				}
			}

			foreach (var desc in element.Descendants()) {
				foreach (var attr in desc.GetAttributes()) {
					string? val = attr.Value;
					if ((attr.LocalName == "embed" || attr.LocalName == "id") && val != null && val.StartsWith("rId")) {
						return val;
					}
				}
				foreach (var attr in desc.ExtendedAttributes) {
					string? val = attr.Value;
					if ((attr.LocalName == "embed" || attr.LocalName == "id") && val != null && val.StartsWith("rId")) {
						return val;
					}
				}
			}

			return null;
		}
	}
}
