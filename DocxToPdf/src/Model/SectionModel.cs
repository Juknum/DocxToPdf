using System.Collections.Generic;

namespace DocxToPdf.Model {
	public class SectionModel {
		public PageSetupModel PageSetup { get; set; } = new PageSetupModel();

		public HeaderFooterModel? HeaderDefault { get; set; }
		public HeaderFooterModel? HeaderFirst { get; set; }
		public HeaderFooterModel? HeaderEven { get; set; }

		public HeaderFooterModel? FooterDefault { get; set; }
		public HeaderFooterModel? FooterFirst { get; set; }
		public HeaderFooterModel? FooterEven { get; set; }

		public List<IBlockElement> Elements { get; set; } = new List<IBlockElement>();
	}
}
