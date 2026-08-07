using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Represents the root model for a parsed DOCX document containing a collection of sections.
	/// </summary>
	public class DocumentModel {
		/// <summary>
		/// Gets or sets the collection of sections contained within the document.
		/// </summary>
		public List<SectionModel> Sections { get; set; } = [];

		/// <summary>
		/// Convenience accessor for single section documents or overall element collection across all sections.
		/// </summary>
		public IEnumerable<IBlockElement> AllElements {
			get {
				foreach (var section in Sections) {
					foreach (var element in section.Elements) {
						yield return element;
					}
				}
			}
		}
	}
}
