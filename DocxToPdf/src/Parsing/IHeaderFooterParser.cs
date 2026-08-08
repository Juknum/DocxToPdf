using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for parsing header and footer parts in a WordprocessingDocument.
	/// </summary>
	public interface IHeaderFooterParser {
		/// <summary>
		/// Parses a header reference into a <see cref="HeaderFooterModel"/>.
		/// </summary>
		/// <param name="headerRef">HeaderReference element. Cannot be null.</param>
		/// <returns>A populated <see cref="HeaderFooterModel"/> or null if header part is missing.</returns>
		HeaderFooterModel? ParseHeader(HeaderReference headerRef);

		/// <summary>
		/// Parses a footer reference into a <see cref="HeaderFooterModel"/>.
		/// </summary>
		/// <param name="footerRef">FooterReference element. Cannot be null.</param>
		/// <returns>A populated <see cref="HeaderFooterModel"/> or null if footer part is missing.</returns>
		HeaderFooterModel? ParseFooter(FooterReference footerRef);
	}
}
