using System;
using System.IO;
using DocxToPdf.Model;

namespace DocxToPdf {
	/// <summary>
	/// Interface defining DOCX parsing and PDF conversion operations.
	/// </summary>
	public interface IConverter {
		/// <summary>
		/// Parses a DOCX file at the specified input path into an object document model (<see cref="DocumentModel"/>).
		/// </summary>
		/// <param name="wordFilePath">The file path to the source DOCX document. Cannot be null.</param>
		/// <returns>A populated <see cref="DocumentModel"/> representing the structure of the document.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="wordFilePath"/> is null.</exception>
		/// <exception cref="FileNotFoundException">Thrown when <paramref name="wordFilePath"/> does not exist.</exception>
		DocumentModel ParseDocument(string wordFilePath);

		/// <summary>
		/// Converts a DOCX file at the specified input path into a rendered PDF file at the target output path.
		/// </summary>
		/// <param name="wordFilePath">The file path to the source DOCX document. Cannot be null.</param>
		/// <param name="outputPdfFilePath">The destination file path for the generated PDF document. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="wordFilePath"/> or <paramref name="outputPdfFilePath"/> is null.</exception>
		/// <exception cref="FileNotFoundException">Thrown when <paramref name="wordFilePath"/> does not exist.</exception>
		void ConvertDocument(string wordFilePath, string outputPdfFilePath);
	}
}
