using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for parsing OpenXML <see cref="Table"/> structures into internal <see cref="TableModel"/> instances.
	/// </summary>
	public interface ITableParser {
		/// <summary>
		/// Parses an OpenXML <see cref="Table"/> into a <see cref="TableModel"/>.
		/// </summary>
		/// <param name="tbl">The OpenXML Table element. Cannot be null.</param>
		/// <param name="mediaResolver">The media resolver instance. Cannot be null.</param>
		/// <param name="paragraphParser">Optional paragraph parser instance for cell content.</param>
		/// <returns>A populated <see cref="TableModel"/>.</returns>
		TableModel ParseTable(Table? tbl, MediaResolver? mediaResolver, IParagraphParser? paragraphParser = null);
	}
}
