using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Extracts binary image data and vector shape properties from OpenXML drawing elements.
	/// </summary>
	/// <param name="partContainer">The part container holding relationship image parts.</param>
	public class MediaResolver(OpenXmlPartContainer partContainer) {
		private readonly OpenXmlPartContainer _partContainer = partContainer ?? throw new ArgumentNullException(nameof(partContainer));

		/// <summary>
		/// Extracts a <see cref="DrawingModel"/> from an OpenXML <see cref="Drawing"/> element.
		/// </summary>
		/// <param name="drawing">The OpenXML Drawing element.</param>
		/// <returns>A populated <see cref="DrawingModel"/> or null if element is invalid.</returns>
		public DrawingModel? ExtractDrawing(Drawing drawing) {
			if (drawing == null) return null;

			Wp.Inline? inline = drawing.GetFirstChild<Wp.Inline>();
			Wp.Anchor? anchor = drawing.GetFirstChild<Wp.Anchor>();

			long cx = 0;
			long cy = 0;
			long offsetXEmu = 0;
			long offsetYEmu = 0;
			bool behindDoc = false;
			DrawingPlacement placement = DrawingPlacement.Inline;

			string? relHStr = null;
			string? relVStr = null;
			string? alignH = null;
			string? alignV = null;

			if (inline != null) {
				placement = DrawingPlacement.Inline;
				if (inline.Extent != null) {
					cx = inline.Extent.Cx?.Value ?? 0;
					cy = inline.Extent.Cy?.Value ?? 0;
				}
			} else if (anchor != null) {
				placement = DrawingPlacement.Floating;
				if (anchor.Extent != null) {
					cx = anchor.Extent.Cx?.Value ?? 0;
					cy = anchor.Extent.Cy?.Value ?? 0;
				}
				var xfrmExt = drawing.Descendants<A.Extents>().FirstOrDefault();
				if (xfrmExt != null && xfrmExt.Cx?.Value > 0 && xfrmExt.Cy?.Value > 0) {
					cx = xfrmExt.Cx.Value;
					cy = xfrmExt.Cy.Value;
				}
				behindDoc = anchor.BehindDoc?.Value == true;

				Wp.HorizontalPosition? posH = anchor.GetFirstChild<Wp.HorizontalPosition>();
				if (posH != null) {
					if (posH.RelativeFrom != null) {
						relHStr = posH.RelativeFrom.InnerText?.ToLowerInvariant() ?? "margin";
					}
					Wp.PositionOffset? offsetH = posH.GetFirstChild<Wp.PositionOffset>();
					if (offsetH != null && long.TryParse(offsetH.Text, out long hVal)) {
						offsetXEmu = hVal;
					}
					Wp.HorizontalAlignment? alignmentH = posH.GetFirstChild<Wp.HorizontalAlignment>();
					if (alignmentH != null) {
						alignH = alignmentH.Text?.ToLowerInvariant();
					}
				}

				Wp.VerticalPosition? posV = anchor.GetFirstChild<Wp.VerticalPosition>();
				if (posV != null) {
					if (posV.RelativeFrom != null) {
						relVStr = posV.RelativeFrom.InnerText?.ToLowerInvariant() ?? "margin";
					}
					Wp.PositionOffset? offsetV = posV.GetFirstChild<Wp.PositionOffset>();
					if (offsetV != null && long.TryParse(offsetV.Text, out long vVal)) {
						offsetYEmu = vVal;
					}
					Wp.VerticalAlignment? alignmentV = posV.GetFirstChild<Wp.VerticalAlignment>();
					if (alignmentV != null) {
						alignV = alignmentV.Text?.ToLowerInvariant();
					}
				}
			}

				string? relationshipId = FindBlipRelationshipId(drawing);
				byte[]? imageData = null;
				string contentType = "image/png";

				if (!string.IsNullOrEmpty(relationshipId)) {
					try {
						ImagePart? imagePart = _partContainer.GetPartById(relationshipId!) as ImagePart;
						if (imagePart != null) {
							using Stream stream = imagePart.GetStream();
							using MemoryStream ms = new MemoryStream();
							stream.CopyTo(ms);
							imageData = ms.ToArray();
							contentType = imagePart.ContentType ?? "image/png";
						}
					} catch { }
				}

				string? fillColorHex = FindSolidFillColor(drawing);
				string? borderColorHex = FindBorderColor(drawing);
				bool hasTextbox = drawing.Descendants<Paragraph>().Any();

				if ((imageData == null || imageData.Length == 0) && string.IsNullOrEmpty(fillColorHex) && string.IsNullOrEmpty(borderColorHex) && !hasTextbox) {
					return null;
				}

				return new DrawingModel {
					RelationshipId = relationshipId ?? string.Empty,
					ImageData = imageData ?? System.Array.Empty<byte>(),
					ContentType = contentType,
					WidthPt = TwipConverter.EmusToPoints(cx),
					HeightPt = TwipConverter.EmusToPoints(cy),
					Placement = placement,
					OffsetXPt = TwipConverter.EmusToPoints(offsetXEmu),
					OffsetYPt = TwipConverter.EmusToPoints(offsetYEmu),
					AlignH = alignH,
					AlignV = alignV,
					BehindDoc = behindDoc,
					HorizontalRelativeFrom = relHStr,
					VerticalRelativeFrom = relVStr,
					FillColorHex = fillColorHex,
					BorderColorHex = borderColorHex,
					ZIndex = anchor?.RelativeHeight?.Value ?? 0
				};
		}

		/// <summary>
		/// Extracts a <see cref="DrawingModel"/> from legacy VML <see cref="Picture"/> elements.
		/// </summary>
		/// <param name="pict">The OpenXML Picture element.</param>
		/// <returns>A populated <see cref="DrawingModel"/> or null if unresolvable.</returns>
		public DrawingModel? ExtractPict(Picture pict) {
			if (pict == null) return null;

			string? relationshipId = FindBlipRelationshipId(pict);
			if (string.IsNullOrEmpty(relationshipId)) {
				return null;
			}

			return ExtractImageByRelationshipId(relationshipId!, DrawingPlacement.Inline, 0, 0);
		}

		/// <summary>
		/// Extracts binary image data by relationship ID and creates a <see cref="DrawingModel"/>.
		/// </summary>
		/// <param name="relationshipId">OpenXML relationship ID string.</param>
		/// <param name="placement">Drawing placement (Inline or Floating).</param>
		/// <param name="cx">Width in EMUs.</param>
		/// <param name="cy">Height in EMUs.</param>
		/// <returns>A populated <see cref="DrawingModel"/> or null if image part is missing.</returns>
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

		private string? FindSolidFillColor(OpenXmlElement container) {
			if (container.Descendants<A.NoFill>().Any(n => !n.Ancestors().Any(a => a.LocalName == "ln" || a.LocalName == "outline"))) {
				return null;
			}
			foreach (var srgbClr in container.Descendants<A.RgbColorModelHex>()) {
				if (srgbClr.Ancestors().Any(a => a.LocalName == "ln" || a.LocalName == "outline")) continue;
				if (!string.IsNullOrEmpty(srgbClr.Val?.Value)) {
					return srgbClr.Val.Value;
				}
			}
			return null;
		}

		private string? FindBorderColor(OpenXmlElement container) {
			foreach (var outline in container.Descendants<A.Outline>()) {
				var srgbClr = outline.Descendants<A.RgbColorModelHex>().FirstOrDefault();
				if (!string.IsNullOrEmpty(srgbClr?.Val?.Value)) {
					return srgbClr.Val.Value;
				}
			}
			return null;
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
