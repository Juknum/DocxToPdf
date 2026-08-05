using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using ModelHeaderFooterType = DocxToPdf.Model.HeaderFooterType;

namespace DocxToPdf.Parsing {
	public class HeaderFooterParser {
		private readonly WordprocessingDocument _wordDoc;
		private readonly ParagraphParser _paragraphParser;
		private readonly TableParser _tableParser;

		public HeaderFooterParser(WordprocessingDocument wordDoc, ParagraphParser paragraphParser, TableParser tableParser) {
			_wordDoc = wordDoc;
			_paragraphParser = paragraphParser;
			_tableParser = tableParser;
		}

		public HeaderFooterModel? ParseHeader(HeaderReference headerRef) {
			if (headerRef?.Id?.Value == null) return null;

			HeaderPart? headerPart = _wordDoc.MainDocumentPart?.GetPartById(headerRef.Id.Value) as HeaderPart;
			if (headerPart?.Header == null) return null;

			ModelHeaderFooterType type = MapType(headerRef.Type?.Value);
			HeaderFooterModel model = new HeaderFooterModel { Type = type };

			MediaResolver mediaResolver = new MediaResolver(headerPart);
			ParseContainerElements(headerPart.Header, model.Elements, mediaResolver);

			return model;
		}

		public HeaderFooterModel? ParseFooter(FooterReference footerRef) {
			if (footerRef?.Id?.Value == null) return null;

			FooterPart? footerPart = _wordDoc.MainDocumentPart?.GetPartById(footerRef.Id.Value) as FooterPart;
			if (footerPart?.Footer == null) return null;

			ModelHeaderFooterType type = MapType(footerRef.Type?.Value);
			HeaderFooterModel model = new HeaderFooterModel { Type = type };

			MediaResolver mediaResolver = new MediaResolver(footerPart);
			ParseContainerElements(footerPart.Footer, model.Elements, mediaResolver);

			return model;
		}

		private void ParseContainerElements(OpenXmlCompositeElement container, List<IBlockElement> targetList, MediaResolver mediaResolver) {
			foreach (var child in container.ChildElements) {
				if (child is Paragraph p) {
					targetList.Add(_paragraphParser.ParseParagraph(p, mediaResolver));
				} else if (child is Table tbl) {
					targetList.Add(_tableParser.ParseTable(tbl, mediaResolver));
				}
			}
		}

		private ModelHeaderFooterType MapType(HeaderFooterValues? value) {
			if (value == HeaderFooterValues.First) return ModelHeaderFooterType.FirstPage;
			if (value == HeaderFooterValues.Even) return ModelHeaderFooterType.EvenPage;
			return ModelHeaderFooterType.Default;
		}
	}
}
