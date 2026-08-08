using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Vml;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for resolving embedded media image streams and drawing objects from OpenXML parts.
	/// </summary>
	public interface IMediaResolver {
		/// <summary>
		/// Extracts a <see cref="DrawingModel"/> from an OpenXML <see cref="Drawing"/> element.
		/// </summary>
		/// <param name="drawing">The OpenXML Drawing element.</param>
		/// <returns>A populated <see cref="DrawingModel"/> or null if drawing object cannot be resolved.</returns>
		DrawingModel? ExtractDrawing(Drawing drawing);

		/// <summary>
		/// Extracts a <see cref="DrawingModel"/> from legacy VML <see cref="Picture"/> elements.
		/// </summary>
		/// <param name="pict">The OpenXML Picture element.</param>
		/// <returns>A populated <see cref="DrawingModel"/> or null if unresolvable.</returns>
		DrawingModel? ExtractPict(Picture pict);

		/// <summary>
		/// Extracts binary image data by relationship ID and creates a <see cref="DrawingModel"/>.
		/// </summary>
		/// <param name="relationshipId">OpenXML relationship ID string.</param>
		/// <param name="placement">Drawing placement (Inline or Floating).</param>
		/// <param name="cx">Width in EMUs.</param>
		/// <param name="cy">Height in EMUs.</param>
		/// <returns>A populated <see cref="DrawingModel"/> or null if image part is missing.</returns>
		DrawingModel? ExtractImageByRelationshipId(string relationshipId, DrawingPlacement placement, long cx, long cy);
	}
}
