using System.Collections.Generic;

namespace DocxToPdf.Model {
	public enum HeaderFooterType {
		Default,
		FirstPage,
		EvenPage
	}

	public class HeaderFooterModel {
		public HeaderFooterType Type { get; set; } = HeaderFooterType.Default;
		public List<IBlockElement> Elements { get; set; } = new List<IBlockElement>();
	}
}
