using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Model representing a document section with its own page setup, headers/footers, and body block elements.
	/// </summary>
	public class SectionModel {
		/// <summary>Gets or sets page setup options (dimensions, orientation, margins) for this section.</summary>
		public PageSetupModel PageSetup { get; set; } = new();

		/// <summary>Gets or sets default header for standard pages in this section.</summary>
		public HeaderFooterModel? HeaderDefault { get; set; }

		/// <summary>Gets or sets header for the first page in this section.</summary>
		public HeaderFooterModel? HeaderFirst { get; set; }

		/// <summary>Gets or sets header for even-numbered pages in this section.</summary>
		public HeaderFooterModel? HeaderEven { get; set; }

		/// <summary>Gets or sets default footer for standard pages in this section.</summary>
		public HeaderFooterModel? FooterDefault { get; set; }

		/// <summary>Gets or sets footer for the first page in this section.</summary>
		public HeaderFooterModel? FooterFirst { get; set; }

		/// <summary>Gets or sets footer for even-numbered pages in this section.</summary>
		public HeaderFooterModel? FooterEven { get; set; }

		/// <summary>Gets or sets the list of block-level body elements (paragraphs, tables, drawings) in this section.</summary>
		public List<IBlockElement> Elements { get; set; } = [];
	}
}
