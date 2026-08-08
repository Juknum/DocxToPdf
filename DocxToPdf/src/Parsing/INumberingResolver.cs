using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for resolving list numbering definitions and bullet text formatting from WordprocessingDocument instances.
	/// </summary>
	public interface INumberingResolver {
		/// <summary>
		/// Resolves list format model details for a given OpenXML <see cref="NumberingProperties"/> element.
		/// </summary>
		/// <param name="numPr">The NumberingProperties element from a paragraph.</param>
		/// <returns>A populated <see cref="ListFormatModel"/> or null if numbering is unresolvable.</returns>
		ListFormatModel? ResolveNumbering(NumberingProperties? numPr);

		/// <summary>
		/// Resolves paragraph numbering properties into a <see cref="ListFormatModel"/>.
		/// </summary>
		/// <param name="numPr">The OpenXML NumberingProperties element.</param>
		/// <returns>A populated <see cref="ListFormatModel"/> or null if numbering properties are missing.</returns>
		ListFormatModel? ResolveListFormat(NumberingProperties? numPr);
	}
}
