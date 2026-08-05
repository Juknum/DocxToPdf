using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class ModelAndSectionTests {
		[Fact]
		public void TestDocumentModelAllElementsIterator() {
			DocumentModel docModel = new DocumentModel();

			SectionModel s1 = new SectionModel();
			s1.Elements.Add(new ParagraphModel());
			s1.Elements.Add(new TableModel());

			SectionModel s2 = new SectionModel();
			s2.Elements.Add(new ParagraphModel());

			docModel.Sections.Add(s1);
			docModel.Sections.Add(s2);

			var all = docModel.AllElements.ToList();
			Assert.Equal(3, all.Count);
		}

		[Fact]
		public void TestPageSetupModelPrintableArea() {
			PageSetupModel setup = new PageSetupModel {
				Width = 612.0,
				Height = 792.0,
				Orientation = PageOrientation.Landscape,
				Margins = new PageMarginsModel {
					Left = 36.0,
					Right = 36.0,
					Top = 54.0,
					Bottom = 54.0,
					Header = 18.0,
					Footer = 18.0
				}
			};

			Assert.Equal(540.0, setup.PrintableWidth);
			Assert.Equal(684.0, setup.PrintableHeight);
			Assert.Equal(PageOrientation.Landscape, setup.Orientation);
		}

		[Fact]
		public void TestHeaderFooterFirstEvenAndTableInHeader() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					// Header 1 (First)
					HeaderPart headerFirstPart = mainPart.AddNewPart<HeaderPart>("rIdHeaderFirst");
					headerFirstPart.Header = new Header(
						new Paragraph(new Run(new Text("First Header")))
					);

					// Header 2 (Even) with a Table
					HeaderPart headerEvenPart = mainPart.AddNewPart<HeaderPart>("rIdHeaderEven");
					headerEvenPart.Header = new Header(
						new Table(
							new TableRow(new TableCell(new Paragraph(new Run(new Text("Even Table Header")))))
						)
					);

					// Footer 1 (First)
					FooterPart footerFirstPart = mainPart.AddNewPart<FooterPart>("rIdFooterFirst");
					footerFirstPart.Footer = new Footer(
						new Paragraph(new Run(new Text("First Footer")))
					);

					// Footer 2 (Even)
					FooterPart footerEvenPart = mainPart.AddNewPart<FooterPart>("rIdFooterEven");
					footerEvenPart.Footer = new Footer(
						new Paragraph(new Run(new Text("Even Footer")))
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(
							new ParagraphProperties(
								new SectionProperties(
									new HeaderReference { Id = "rIdHeaderFirst", Type = HeaderFooterValues.First },
									new FooterReference { Id = "rIdFooterFirst", Type = HeaderFooterValues.First },
									new PageSize { Width = 15840, Height = 12240, Orient = PageOrientationValues.Landscape }
								)
							),
							new Run(new Text("Section 1 Paragraph"))
						),
						new Paragraph(new Run(new Text("Section 2 Paragraph"))),
						new SectionProperties(
							new HeaderReference { Id = "rIdHeaderEven", Type = HeaderFooterValues.Even },
							new FooterReference { Id = "rIdFooterEven", Type = HeaderFooterValues.Even }
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					Assert.Equal(2, model.Sections.Count);

					// Section 1
					SectionModel s1 = model.Sections[0];
					Assert.Equal(PageOrientation.Landscape, s1.PageSetup.Orientation);
					Assert.Equal(792.0, s1.PageSetup.Width);
					Assert.Equal(612.0, s1.PageSetup.Height);
					Assert.NotNull(s1.HeaderFirst);
					Assert.NotNull(s1.FooterFirst);

					ParagraphModel firstHeaderPara = Assert.IsType<ParagraphModel>(s1.HeaderFirst!.Elements[0]);
					Assert.Equal("First Header", firstHeaderPara.Runs[0].Text);

					// Section 2
					SectionModel s2 = model.Sections[1];
					Assert.NotNull(s2.HeaderEven);
					Assert.NotNull(s2.FooterEven);

					TableModel evenHeaderTable = Assert.IsType<TableModel>(s2.HeaderEven!.Elements[0]);
					TableCellModel cell = evenHeaderTable.Rows[0].Cells[0];
					ParagraphModel cellPara = Assert.IsType<ParagraphModel>(cell.Elements[0]);
					Assert.Equal("Even Table Header", cellPara.Runs[0].Text);
				}
			}
		}
	}
}
