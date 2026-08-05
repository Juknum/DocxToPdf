using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class NumberingAndHeaderTests {

		[Fact]
		public void TestNumberingAndHeaderFooterParsing() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					// Add HeaderPart
					HeaderPart headerPart = mainPart.AddNewPart<HeaderPart>("rIdHeader1");
					headerPart.Header = new Header(
						new Paragraph(new Run(new Text("Header Title")))
					);

					// Add FooterPart with PAGE simple field
					FooterPart footerPart = mainPart.AddNewPart<FooterPart>("rIdFooter1");
					footerPart.Footer = new Footer(
						new Paragraph(
							new Run(new Text("Page ")),
							new SimpleField { Instruction = "PAGE" }
						)
					);

					// Add NumberingDefinitionsPart
					NumberingDefinitionsPart numPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
					numPart.Numbering = new Numbering(
						new AbstractNum(
							new Level(
								new NumberingFormat { Val = NumberFormatValues.Decimal },
								new LevelText { Val = "%1." }
							) { LevelIndex = 0 }
						) { AbstractNumberId = 1 },
						new NumberingInstance(
							new AbstractNumId { Val = 1 }
						) { NumberID = 1 }
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(
							new ParagraphProperties(
								new NumberingProperties(
									new NumberingId { Val = 1 },
									new NumberingLevelReference { Val = 0 }
								)
							),
							new Run(new Text("First List Item"))
						),
						new Paragraph(
							new ParagraphProperties(
								new NumberingProperties(
									new NumberingId { Val = 1 },
									new NumberingLevelReference { Val = 0 }
								)
							),
							new Run(new Text("Second List Item"))
						),
						new SectionProperties(
							new HeaderReference { Id = "rIdHeader1", Type = HeaderFooterValues.Default },
							new FooterReference { Id = "rIdFooter1", Type = HeaderFooterValues.Default }
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					SectionModel section = Assert.Single(model.Sections);
					Assert.NotNull(section.HeaderDefault);
					Assert.NotNull(section.FooterDefault);

					ParagraphModel headerPara = Assert.IsType<ParagraphModel>(section.HeaderDefault!.Elements[0]);
					Assert.Equal("Header Title", headerPara.Runs[0].Text);

					ParagraphModel footerPara = Assert.IsType<ParagraphModel>(section.FooterDefault!.Elements[0]);
					Assert.Equal(FieldType.PageNumber, footerPara.Runs[1].Field);

					ParagraphModel item1 = Assert.IsType<ParagraphModel>(section.Elements[0]);
					Assert.NotNull(item1.ListFormat);
					Assert.Equal("1.", item1.ListFormat!.MarkerText);

					ParagraphModel item2 = Assert.IsType<ParagraphModel>(section.Elements[1]);
					Assert.NotNull(item2.ListFormat);
					Assert.Equal("2.", item2.ListFormat!.MarkerText);
				}
			}
		}
	}
}
