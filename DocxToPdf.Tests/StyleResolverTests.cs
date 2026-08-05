using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class StyleResolverTests {

		[Fact]
		public void TestStyleInheritanceAndCascade() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					
					// Add styles.xml
					StyleDefinitionsPart stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
					stylePart.Styles = new Styles(
						new DocDefaults(
							new RunPropertiesDefault(new RunPropertiesBaseStyle(
								new RunFonts { Ascii = "Arial" },
								new FontSize { Val = "20" } // 10pt
							)),
							new ParagraphPropertiesDefault()
						),
						// Base Paragraph Style
						new Style(
							new StyleName { Val = "Heading Base" },
							new StyleParagraphProperties(new Justification { Val = JustificationValues.Center }),
							new StyleRunProperties(new Bold(), new Color { Val = "0000FF" })
						) { StyleId = "HeadingBase", Type = StyleValues.Paragraph },
						// Derived Style basedOn HeadingBase
						new Style(
							new BasedOn { Val = "HeadingBase" },
							new StyleRunProperties(new FontSize { Val = "32" }) // 16pt
						) { StyleId = "Heading1", Type = StyleValues.Paragraph }
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(
							new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
							new Run(new Text("Header Text"))
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					ParagraphModel p = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[0]);
					Assert.Equal(ParagraphAlignment.Center, p.Alignment);

					RunModel run = p.Runs[0];
					Assert.Equal("Arial", run.FontFamily);
					Assert.True(run.IsBold);
					Assert.Equal(16.0, run.FontSizePt); // Overridden by Heading1
					Assert.Equal("#0000FF", run.TextColorHex); // Inherited from HeadingBase
				}
			}
		}
	}
}
