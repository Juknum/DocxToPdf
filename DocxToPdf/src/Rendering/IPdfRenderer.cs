using System;
using DocxToPdf.Model;
using PdfSharp.Pdf;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Interface for rendering a parsed <see cref="DocumentModel"/> into a PDF document.
	/// </summary>
	public interface IPdfRenderer {
		/// <summary>
		/// Renders a document model into a PDF document (<see cref="PdfDocument"/>).
		/// </summary>
		/// <param name="documentModel">The document model to render. Cannot be null.</param>
		/// <returns>A populated <see cref="PdfDocument"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="documentModel"/> is null.</exception>
		PdfDocument RenderDocument(DocumentModel documentModel);
	}
}
