using System.Collections.Generic;

namespace DocxToPdf.Model {
	/// <summary>
	/// Specifies the type of header or footer (Default, FirstPage, or EvenPage).
	/// </summary>
	public enum HeaderFooterType {
		/// <summary>Default header/footer applied to standard document pages.</summary>
		Default,
		/// <summary>Special header/footer applied exclusively to the first page of a section.</summary>
		FirstPage,
		/// <summary>Special header/footer applied to even-numbered pages when different odd/even pages are enabled.</summary>
		EvenPage
	}

	/// <summary>
	/// Represents a header or footer container model containing block-level elements.
	/// </summary>
	public class HeaderFooterModel {
		/// <summary>Gets or sets the type of header or footer.</summary>
		public HeaderFooterType Type { get; set; } = HeaderFooterType.Default;

		/// <summary>Gets or sets the list of block-level elements contained within the header/footer.</summary>
		public List<IBlockElement> Elements { get; set; } = [];
	}
}
