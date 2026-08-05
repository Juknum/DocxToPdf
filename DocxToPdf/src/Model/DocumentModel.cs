using System.Collections.Generic;

namespace DocxToPdf.Model {
	public class DocumentModel {
		public List<SectionModel> Sections { get; set; } = new List<SectionModel>();

		/// <summary>
		/// Convenience accessor for single section documents or overall element collection.
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
