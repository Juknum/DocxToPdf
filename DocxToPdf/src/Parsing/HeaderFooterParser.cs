using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using ModelHeaderFooterType = DocxToPdf.Model.HeaderFooterType;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Parses header and footer parts from an OpenXML document into <see cref="HeaderFooterModel"/> instances.
	/// </summary>
	/// <param name="wordDoc">The WordprocessingDocument package.</param>
	/// <param name="paragraphParser">The ParagraphParser instance.</param>
	/// <param name="tableParser">The TableParser instance.</param>
	public class HeaderFooterParser(WordprocessingDocument wordDoc, ParagraphParser paragraphParser, TableParser tableParser) {
		private readonly WordprocessingDocument _wordDoc = wordDoc ?? throw new ArgumentNullException(nameof(wordDoc));
		private readonly ParagraphParser _paragraphParser = paragraphParser ?? throw new ArgumentNullException(nameof(paragraphParser));
		private readonly TableParser _tableParser = tableParser ?? throw new ArgumentNullException(nameof(tableParser));

		/// <summary>
		/// Parses a header reference into a <see cref="HeaderFooterModel"/>.
		/// </summary>
		/// <param name="headerRef">The OpenXML HeaderReference element.</param>
		/// <returns>A populated <see cref="HeaderFooterModel"/> or null if reference cannot be resolved.</returns>
		public HeaderFooterModel? ParseHeader(HeaderReference headerRef) {
			if (headerRef?.Id?.Value == null) return null;

			HeaderPart? headerPart = _wordDoc.MainDocumentPart?.GetPartById(headerRef.Id.Value) as HeaderPart;
			if (headerPart?.Header == null) return null;

			ModelHeaderFooterType type = MapType(headerRef.Type?.Value);
			HeaderFooterModel model = new() { Type = type };

			MediaResolver mediaResolver = new(headerPart);
			ParseContainerElements(headerPart.Header, model.Elements, mediaResolver);

			return model;
		}

		/// <summary>
		/// Parses a footer reference into a <see cref="HeaderFooterModel"/>.
		/// </summary>
		/// <param name="footerRef">The OpenXML FooterReference element.</param>
		/// <returns>A populated <see cref="HeaderFooterModel"/> or null if reference cannot be resolved.</returns>
		public HeaderFooterModel? ParseFooter(FooterReference footerRef) {
			if (footerRef?.Id?.Value == null) return null;

			FooterPart? footerPart = _wordDoc.MainDocumentPart?.GetPartById(footerRef.Id.Value) as FooterPart;
			if (footerPart?.Footer == null) return null;

			ModelHeaderFooterType type = MapType(footerRef.Type?.Value);
			HeaderFooterModel model = new() { Type = type };

			MediaResolver mediaResolver = new(footerPart);
			ParseContainerElements(footerPart.Footer, model.Elements, mediaResolver);

			return model;
		}

		private void ParseContainerElements(OpenXmlCompositeElement container, List<IBlockElement> targetList, MediaResolver mediaResolver) {
			foreach (var child in container.ChildElements) {
				if (child is Paragraph p) {
					targetList.Add(_paragraphParser.ParseParagraph(p, mediaResolver));
				} else if (child is Table tbl) {
					targetList.Add(_tableParser.ParseTable(tbl, mediaResolver, _paragraphParser));
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
