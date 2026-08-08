using System;
using DocumentFormat.OpenXml.Packaging;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Interface for parsing OpenXML <see cref="WordprocessingDocument"/> packages into internal <see cref="DocumentModel"/> domain instances.
	/// </summary>
	public interface IDocxParser {
		/// <summary>
		/// Parses a WordprocessingDocument package into an in-memory object model.
		/// </summary>
		/// <param name="wordDoc">The input WordprocessingDocument package. Cannot be null.</param>
		/// <returns>A populated <see cref="DocumentModel"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="wordDoc"/> is null.</exception>
		DocumentModel ParseDocument(WordprocessingDocument? wordDoc);
	}
}
