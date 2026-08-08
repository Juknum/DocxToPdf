using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using ModelHeaderFooterType = DocxToPdf.Model.HeaderFooterType;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Provides functionality to parse OpenXML <see cref="WordprocessingDocument"/> structures into an in-memory <see cref="DocumentModel"/>.
	/// </summary>
	public class DocxParser : IDocxParser {
		/// <inheritdoc />
		public DocumentModel ParseDocument(WordprocessingDocument wordDoc) => Parse(wordDoc);

		/// <inheritdoc />
		public static DocumentModel Parse(WordprocessingDocument wordDoc) {
			if (wordDoc == null) throw new ArgumentNullException(nameof(wordDoc));

			if (wordDoc.MainDocumentPart?.Document?.Body == null) {
				return new DocumentModel();
			}

			StyleResolver styleResolver = new(wordDoc);
			NumberingResolver numberingResolver = new(wordDoc);
			MediaResolver mainMediaResolver = new(wordDoc.MainDocumentPart);

			ParagraphParser paragraphParser = new(styleResolver, numberingResolver);
			TableParser tableParser = new(styleResolver);
			HeaderFooterParser headerFooterParser = new(wordDoc, paragraphParser, tableParser);

			DocumentModel documentModel = new();
			Body body = wordDoc.MainDocumentPart.Document.Body;

			// Word documents can have multiple sections.
			// Body level elements before a section break belong to that section.
			SectionModel currentSection = new();
			documentModel.Sections.Add(currentSection);

			foreach (var element in body.ChildElements) {
				if (element is Paragraph p) {
					var pElements = paragraphParser.ParseParagraphToElements(p, mainMediaResolver, tableParser);
					currentSection.Elements.AddRange(pElements);

					// Check if paragraph contains a SectionProperties break (w:pPr/w:sectPr)
					SectionProperties? pSectPr = p.ParagraphProperties?.SectionProperties;
					if (pSectPr != null) {
						ApplySectionProperties(currentSection, pSectPr, headerFooterParser);
						
						// Prepare next section
						currentSection = new SectionModel();
						documentModel.Sections.Add(currentSection);
					}
				} else if (element is Table tbl) {
					TableModel tblModel = tableParser.ParseTable(tbl, mainMediaResolver, paragraphParser);
					currentSection.Elements.Add(tblModel);
				} else if (element is SectionProperties sectPr) {
					// Final body section properties
					ApplySectionProperties(currentSection, sectPr, headerFooterParser);
				}
			}

			// If the last section has no elements and no page setup custom configuration, and there are prior sections, clean it up
			if (documentModel.Sections.Count > 1 && currentSection.Elements.Count == 0) {
				documentModel.Sections.Remove(currentSection);
			}

			return documentModel;
		}

		private static void ApplySectionProperties(SectionModel section, SectionProperties sectPr, HeaderFooterParser headerFooterParser) {
			section.PageSetup = ParsePageSetup(sectPr);

			foreach (HeaderReference headerRef in sectPr.Elements<HeaderReference>()) {
				HeaderFooterModel? header = headerFooterParser.ParseHeader(headerRef);
				if (header != null) {
					switch (header.Type) {
						case ModelHeaderFooterType.FirstPage: section.HeaderFirst = header; break;
						case ModelHeaderFooterType.EvenPage: section.HeaderEven = header; break;
						default: section.HeaderDefault = header; break;
					}
				}
			}

			foreach (FooterReference footerRef in sectPr.Elements<FooterReference>()) {
				HeaderFooterModel? footer = headerFooterParser.ParseFooter(footerRef);
				if (footer != null) {
					switch (footer.Type) {
						case ModelHeaderFooterType.FirstPage: section.FooterFirst = footer; break;
						case ModelHeaderFooterType.EvenPage: section.FooterEven = footer; break;
						default: section.FooterDefault = footer; break;
					}
				}
			}
		}

		private static PageSetupModel ParsePageSetup(SectionProperties sectPr) {
			PageSetupModel pageSetup = new PageSetupModel();

			// Page Size (w:pgSz)
			PageSize? pgSz = sectPr.GetFirstChild<PageSize>();
			if (pgSz != null) {
				if (pgSz.Width?.Value != null) {
					pageSetup.Width = TwipConverter.TwipsToPoints(pgSz.Width.Value);
				}
				if (pgSz.Height?.Value != null) {
					pageSetup.Height = TwipConverter.TwipsToPoints(pgSz.Height.Value);
				}
				if (pgSz.Orient?.Value == PageOrientationValues.Landscape) {
					pageSetup.Orientation = PageOrientation.Landscape;
				}
			}

			// Page Margin (w:pgMar)
			PageMargin? pgMar = sectPr.GetFirstChild<PageMargin>();
			if (pgMar != null) {
				if (pgMar.Top?.Value != null) {
					pageSetup.Margins.Top = TwipConverter.TwipsToPoints(pgMar.Top.Value);
				}
				if (pgMar.Bottom?.Value != null) {
					pageSetup.Margins.Bottom = TwipConverter.TwipsToPoints(pgMar.Bottom.Value);
				}
				if (pgMar.Left?.Value != null) {
					pageSetup.Margins.Left = TwipConverter.TwipsToPoints(pgMar.Left.Value);
				}
				if (pgMar.Right?.Value != null) {
					pageSetup.Margins.Right = TwipConverter.TwipsToPoints(pgMar.Right.Value);
				}
				if (pgMar.Header?.Value != null) {
					pageSetup.Margins.Header = TwipConverter.TwipsToPoints(pgMar.Header.Value);
				}
				if (pgMar.Footer?.Value != null) {
					pageSetup.Margins.Footer = TwipConverter.TwipsToPoints(pgMar.Footer.Value);
				}
			}

			return pageSetup;
		}
	}
}
