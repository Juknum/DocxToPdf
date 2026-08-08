using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for parsing OpenXML <see cref="Paragraph"/> elements into internal <see cref="ParagraphModel"/> and block element structures.
	/// </summary>
	public interface IParagraphParser {
		/// <summary>
		/// Parses an OpenXML <see cref="Paragraph"/> into a <see cref="ParagraphModel"/>.
		/// </summary>
		/// <param name="p">The OpenXML Paragraph element. Cannot be null.</param>
		/// <param name="mediaResolver">The media resolver instance. Cannot be null.</param>
		/// <returns>A populated <see cref="ParagraphModel"/>.</returns>
		ParagraphModel ParseParagraph(Paragraph p, MediaResolver mediaResolver);

		/// <summary>
		/// Parses an OpenXML <see cref="Paragraph"/> into a list of block elements.
		/// </summary>
		/// <param name="p">The OpenXML Paragraph element. Cannot be null.</param>
		/// <param name="mediaResolver">The media resolver instance. Cannot be null.</param>
		/// <param name="tableParser">Optional TableParser instance for nested table extraction.</param>
		/// <returns>A list of parsed <see cref="IBlockElement"/> instances.</returns>
		List<IBlockElement> ParseParagraphToElements(Paragraph p, MediaResolver mediaResolver, TableParser? tableParser = null);
	}
}
