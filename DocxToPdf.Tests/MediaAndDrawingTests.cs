using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class MediaAndDrawingTests {
		[Fact]
		public void TestMediaResolverExtractDrawingInlineAndAnchor() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

					// Add dummy image part
					ImagePart imgPart = mainPart.AddImagePart(ImagePartType.Png, "rIdImg1");
					byte[] dummyImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
					using (Stream stream = imgPart.GetStream(FileMode.Create)) {
						stream.Write(dummyImageBytes, 0, dummyImageBytes.Length);
					}

					var inlineDrawing = new Drawing(
						new Wp.Inline(
							new Wp.Extent { Cx = 914400, Cy = 914400 }, // 72pt x 72pt
							new Wp.DocProperties { Id = 1, Name = "Picture 1" },
							new A.Graphic(
								new A.GraphicData(
									new A.Blip { Embed = "rIdImg1" }
								) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
							)
						)
					);

					var anchorDrawing = new Drawing(
						new Wp.Anchor(
							new Wp.Extent { Cx = 1828800, Cy = 914400 }, // 144pt x 72pt
							new Wp.DocProperties { Id = 2, Name = "Picture 2" },
							new A.Graphic(
								new A.GraphicData(
									new A.Blip { Embed = "rIdImg1" }
								) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
							)
						)
					);

					mainPart.Document = new Document(new Body(
						new Paragraph(new Run(inlineDrawing)),
						new Paragraph(new Run(anchorDrawing))
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					MediaResolver resolver = new MediaResolver(wordDoc.MainDocumentPart!);
					
					var inlineDrawing = wordDoc.MainDocumentPart!.Document.Body!
						.Elements<Paragraph>().ElementAt(0)
						.Elements<Run>().ElementAt(0)
						.Elements<Drawing>().ElementAt(0);

					DrawingModel? model1 = resolver.ExtractDrawing(inlineDrawing);
					Assert.NotNull(model1);
					Assert.Equal("rIdImg1", model1!.RelationshipId);
					Assert.Equal(DrawingPlacement.Inline, model1.Placement);
					Assert.Equal(72.0, model1.WidthPt);
					Assert.Equal(72.0, model1.HeightPt);
					Assert.Equal(8, model1.ImageData.Length);

					var anchorDrawing = wordDoc.MainDocumentPart!.Document.Body!
						.Elements<Paragraph>().ElementAt(1)
						.Elements<Run>().ElementAt(0)
						.Elements<Drawing>().ElementAt(0);

					DrawingModel? model2 = resolver.ExtractDrawing(anchorDrawing);
					Assert.NotNull(model2);
					Assert.Equal(DrawingPlacement.Floating, model2!.Placement);
					Assert.Equal(144.0, model2.WidthPt);
				}
			}
		}

		[Fact]
		public void TestParagraphDirectDrawingChild() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					ImagePart imgPart = mainPart.AddImagePart(ImagePartType.Png, "rIdImgDirect");
					byte[] dummyImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
					using (Stream stream = imgPart.GetStream(FileMode.Create)) {
						stream.Write(dummyImageBytes, 0, dummyImageBytes.Length);
					}

					var drawing = new Drawing(
						new Wp.Inline(
							new Wp.Extent { Cx = 914400, Cy = 914400 },
							new A.Graphic(
								new A.GraphicData(
									new A.Blip { Embed = "rIdImgDirect" }
								) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
							)
						)
					);

					// Drawing as direct child of paragraph (not inside Run)
					mainPart.Document = new Document(new Body(
						new Paragraph(drawing)
					));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					DocumentModel model = DocxParser.Parse(wordDoc);
					Assert.Single(model.Sections);
				}
			}
		}

		[Fact]
		public void TestDrawingModelProperties() {
			DrawingModel model = new DrawingModel {
				OffsetXPt = 10.0,
				OffsetYPt = 20.0
			};
			Assert.Equal(10.0, model.OffsetXPt);
			Assert.Equal(20.0, model.OffsetYPt);
		}

		[Fact]
		public void TestMediaResolverNullAndInvalidDrawings() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					mainPart.Document = new Document(new Body());
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					MediaResolver resolver = new MediaResolver(wordDoc.MainDocumentPart!);

					Assert.Null(resolver.ExtractDrawing(null!));

					// Drawing with no children / no relationship
					Drawing emptyDrawing = new Drawing();
					Assert.Null(resolver.ExtractDrawing(emptyDrawing));

					// Drawing with non-existent relationship ID
					Drawing invalidRelDrawing = new Drawing(
						new Wp.Inline(
							new A.Graphic(
								new A.GraphicData(
									new A.Blip { Embed = "rIdNonExistent" }
								) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
							)
						)
					);
					Assert.Null(resolver.ExtractDrawing(invalidRelDrawing));
				}
			}
		}

		[Fact]
		public void TestParagraphDrawingAndTextBoxExtraction() {
			using (MemoryStream ms = new MemoryStream()) {
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document)) {
					MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
					ImagePart imgPart = mainPart.AddImagePart(ImagePartType.Png, "rIdImgBox");
					byte[] dummyImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
					using (Stream stream = imgPart.GetStream(FileMode.Create)) {
						stream.Write(dummyImageBytes, 0, dummyImageBytes.Length);
					}

					var textBoxContent = new TextBoxContent(
						new Paragraph(new Run(new Text("Inside TextBox")))
					);

					var drawing = new Drawing(
						new Wp.Inline(
							new Wp.Extent { Cx = 914400, Cy = 914400 },
							new A.Graphic(
								new A.GraphicData(
									new A.Blip { Embed = "rIdImgBox" }
								) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
							)
						)
					);

					var paragraph = new Paragraph(
						new Run(drawing),
						new Run(new Text("Outside TextBox"))
					);
					// Add textBoxContent as child element
					paragraph.AppendChild(textBoxContent);

					mainPart.Document = new Document(new Body(paragraph));
					wordDoc.Save();
				}

				ms.Position = 0;
				using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false)) {
					MediaResolver resolver = new MediaResolver(wordDoc.MainDocumentPart!);
					var p = wordDoc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
					var drg = p.Descendants<Drawing>().FirstOrDefault();
					Assert.NotNull(drg);
					var drgModel = resolver.ExtractDrawing(drg!);
					Assert.NotNull(drgModel);

					DocumentModel model = DocxParser.Parse(wordDoc);
					Assert.Single(model.Sections);
					var elements = model.Sections[0].Elements;

					Assert.True(elements.Count >= 2);
					Assert.Contains(elements, e => e is DrawingModel);
					Assert.Contains(elements, e => e is ParagraphModel pm && pm.Runs.Any(r => r.Text.Contains("Outside TextBox")));
				}
			}
		}
	}
}
