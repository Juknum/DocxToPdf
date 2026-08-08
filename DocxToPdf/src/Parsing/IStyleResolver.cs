using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for resolving document styles, paragraph formatting, and run typography properties.
	/// </summary>
	public interface IStyleResolver {
		/// <summary>
		/// Resolves paragraph formatting properties across document defaults, named styles, and direct formatting.
		/// </summary>
		/// <param name="pPr">Direct paragraph properties.</param>
		/// <param name="paragraphStyleId">Paragraph style ID string.</param>
		/// <returns>A populated <see cref="ResolvedParagraphStyle"/>.</returns>
		ResolvedParagraphStyle ResolveParagraphStyle(ParagraphProperties? pPr, string? paragraphStyleId);

		/// <summary>
		/// Resolves run formatting properties across document defaults, paragraph styles, run styles, and direct formatting.
		/// </summary>
		/// <param name="rPr">Direct run properties.</param>
		/// <param name="runStyleId">Run style ID string.</param>
		/// <param name="pPr">Direct paragraph properties.</param>
		/// <param name="paragraphStyleId">Paragraph style ID string.</param>
		/// <returns>A populated <see cref="ResolvedRunStyle"/>.</returns>
		ResolvedRunStyle ResolveRunStyle(RunProperties? rPr, string? runStyleId, ParagraphProperties? pPr, string? paragraphStyleId);
	}
}
