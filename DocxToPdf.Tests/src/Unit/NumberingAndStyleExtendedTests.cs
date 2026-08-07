using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class NumberingAndStyleExtendedTests {
		[Fact]
		public void TestNumberingFormatsAndRomanLetters() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					NumberingDefinitionsPart numPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
					numPart.Numbering = new Numbering(
						// AbstractNum 1: Lower Roman starting at 4 (iv)
						new AbstractNum(
							new Level(
								new NumberingFormat { Val = NumberFormatValues.LowerRoman },
								new LevelText { Val = "(%1)" },
								new StartNumberingValue { Val = 4 },
								new PreviousParagraphProperties(
									new Indentation { Left = "720", Hanging = "360" }
								)
							) { LevelIndex = 0 }
						) { AbstractNumberId = 1 },

						// AbstractNum 2: Upper Letter starting at 1 (A)
						new AbstractNum(
							new Level(
								new NumberingFormat { Val = NumberFormatValues.UpperLetter },
								new LevelText { Val = "%1." }
							) { LevelIndex = 0 }
						) { AbstractNumberId = 2 },

						// AbstractNum 3: Bullet custom symbols ("o")
						new AbstractNum(
							new Level(
								new NumberingFormat { Val = NumberFormatValues.Bullet },
								new LevelText { Val = "o" }
							) { LevelIndex = 0 }
						) { AbstractNumberId = 3 },

						// AbstractNum 4: Bullet custom symbols ("v" and "§")
						new AbstractNum(
							new Level(
								new NumberingFormat { Val = NumberFormatValues.Bullet },
								new LevelText { Val = "v" }
							) { LevelIndex = 0 }
						) { AbstractNumberId = 4 },

						// AbstractNum 5: Roman > 1000
						new AbstractNum(
							new Level(
								new NumberingFormat { Val = NumberFormatValues.UpperRoman },
								new LevelText { Val = "%1." },
								new StartNumberingValue { Val = 2024 }
							) { LevelIndex = 0 }
						) { AbstractNumberId = 5 },

						new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 },
						new NumberingInstance(new AbstractNumId { Val = 2 }) { NumberID = 2 },
						new NumberingInstance(new AbstractNumId { Val = 3 }) { NumberID = 3 },
						new NumberingInstance(new AbstractNumId { Val = 4 }) { NumberID = 4 },
						new NumberingInstance(new AbstractNumId { Val = 5 }) { NumberID = 5 }
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 1 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Roman item 1"))
						),
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 1 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Roman item 2"))
						),
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 2 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Letter item 1"))
						),
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 3 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Bullet item 1"))
						),
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 4 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Bullet item 2"))
						),
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 5 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Roman > 1000"))
						),
						// Invalid numId
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 999 }, new NumberingLevelReference { Val = 0 })),
							new Run(new Text("Invalid numId"))
						),
						// Invalid level index
						new Paragraph(
							new ParagraphProperties(new NumberingProperties(new NumberingId { Val = 1 }, new NumberingLevelReference { Val = 99 })),
							new Run(new Text("Invalid level index"))
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);

					ParagraphModel p1 = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[0]);
					Assert.Equal("(iv)", p1.ListFormat!.MarkerText);
					Assert.Equal(ListType.Numbered, p1.ListFormat.Type);

					ParagraphModel p4 = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[3]);
					Assert.Equal("◦", p4.ListFormat!.MarkerText);

					ParagraphModel p5 = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[4]);
					Assert.Equal("▪", p5.ListFormat!.MarkerText);

					ParagraphModel p6 = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[5]);
					Assert.Equal("MMXXIV.", p6.ListFormat!.MarkerText);

					ParagraphModel p7 = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[6]);
					Assert.Null(p7.ListFormat);

					ParagraphModel p8 = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[7]);
					Assert.Null(p8.ListFormat);
				}
			}
		}

		[Fact]
		public void TestParagraphRunAndFieldFormatting() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					// Add Character Styles with BasedOn hierarchy
					StyleDefinitionsPart stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
					stylePart.Styles = new Styles(
						new Style(
							new StyleName { Val = "Base Char Style" },
							new StyleRunProperties(new Color { Val = "112233" })
						) { StyleId = "BaseCharStyle", Type = StyleValues.Character },
						new Style(
							new BasedOn { Val = "BaseCharStyle" },
							new StyleRunProperties(new Bold())
						) { StyleId = "DerivedCharStyle", Type = StyleValues.Character }
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(
							new ParagraphProperties(
								new Justification { Val = JustificationValues.Both },
								new SpacingBetweenLines { Before = "240", After = "120", Line = "360", LineRule = LineSpacingRuleValues.Exact },
								new Indentation { Left = "720", Right = "360", Hanging = "288" },
								new ParagraphMarkRunProperties(new Italic())
							),
							new Run(
								new RunProperties(
									new RunStyle { Val = "DerivedCharStyle" },
									new Strike(),
									new Underline { Val = UnderlineValues.Single },
									new Shading { Fill = "FFFF00" }
								),
								new Text("Formatted Run"),
								new TabChar(),
								new Break()
							),
							// Unknown field code
							new Run(
								new FieldCode { Text = " UNKNOWN_FIELD " }
							),
							// SimpleField with runs inside
							new SimpleField(
								new Run(new RunProperties(new Bold()), new Text("Page "))
							) { Instruction = " PAGE " }
						)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);
					ParagraphModel p = Assert.IsType<ParagraphModel>(model.Sections[0].Elements[0]);

					Assert.Equal(ParagraphAlignment.Justify, p.Alignment);
					Assert.Equal(14.4, p.HangingIndentPt);

					Assert.True(p.Runs[0].IsBold); // From DerivedCharStyle
					Assert.Equal("#112233", p.Runs[0].TextColorHex); // Inherited from BaseCharStyle
					Assert.True(p.Runs[0].IsItalic); // Inherited from ParagraphMarkRunProperties

					// SimpleField with run
					Assert.Equal(FieldType.PageNumber, p.Runs[3].Field);
				}
			}
		}

		[Fact]
		public void TestNumberingResolverNullHandling() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					mainPart.Document = new Document(new Body());
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					NumberingResolver resolver = new NumberingResolver(wordDoc);
					Assert.Null(resolver.ResolveListFormat(null));
					Assert.Null(resolver.ResolveListFormat(new NumberingProperties()));
				}
			}
		}
	}
}
