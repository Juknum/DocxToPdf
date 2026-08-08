using System;
using DocxToPdf;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using DocxToPdf.Rendering;
using Moq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Xunit;

namespace DocxToPdf.Tests.Unit {
	/// <summary>
	/// Unit tests verifying component interface contracts using Moq.
	/// </summary>
	public class InterfaceMoqTests {
		[Fact]
		public void TestConverterInterfaceMocking() {
			var mockConverter = new Mock<IConverter>();
			var expectedModel = new DocumentModel();

			mockConverter.Setup(c => c.ParseDocument("test.docx")).Returns(expectedModel);

			var result = mockConverter.Object.ParseDocument("test.docx");

			Assert.Same(expectedModel, result);
			mockConverter.Verify(c => c.ParseDocument("test.docx"), Times.Once);
		}

		[Fact]
		public void TestStyleResolverInterfaceMocking() {
			var mockStyleResolver = new Mock<IStyleResolver>();
			var expectedStyle = new ResolvedParagraphStyle { SpacingBeforePt = 12.0 };

			mockStyleResolver.Setup(s => s.ResolveParagraphStyle(null, "Heading1")).Returns(expectedStyle);

			var result = mockStyleResolver.Object.ResolveParagraphStyle(null, "Heading1");

			Assert.Equal(12.0, result.SpacingBeforePt);
			mockStyleResolver.Verify(s => s.ResolveParagraphStyle(null, "Heading1"), Times.Once);
		}

		[Fact]
		public void TestNumberingResolverInterfaceMocking() {
			var mockNumberingResolver = new Mock<INumberingResolver>();
			var expectedList = new ListFormatModel { MarkerText = "1." };

			mockNumberingResolver.Setup(n => n.ResolveNumbering(null)).Returns(expectedList);

			var result = mockNumberingResolver.Object.ResolveNumbering(null);

			Assert.NotNull(result);
			Assert.Equal("1.", result.MarkerText);
			mockNumberingResolver.Verify(n => n.ResolveNumbering(null), Times.Once);
		}

		[Fact]
		public void TestParagraphParserInterfaceMocking() {
			var mockParagraphParser = new Mock<IParagraphParser>();
			var expectedPModel = new ParagraphModel();

			mockParagraphParser.Setup(p => p.ParseParagraph(It.IsAny<DocumentFormat.OpenXml.Wordprocessing.Paragraph>(), It.IsAny<MediaResolver>()))
				.Returns(expectedPModel);

			var result = mockParagraphParser.Object.ParseParagraph(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(), null!);

			Assert.Same(expectedPModel, result);
			mockParagraphParser.Verify(p => p.ParseParagraph(It.IsAny<DocumentFormat.OpenXml.Wordprocessing.Paragraph>(), It.IsAny<MediaResolver>()), Times.Once);
		}

		[Fact]
		public void TestPdfRendererInterfaceMocking() {
			var mockPdfRenderer = new Mock<IPdfRenderer>();
			using var expectedPdf = new PdfDocument();

			mockPdfRenderer.Setup(r => r.RenderDocument(It.IsAny<DocumentModel>())).Returns(expectedPdf);

			var result = mockPdfRenderer.Object.RenderDocument(new DocumentModel());

			Assert.Same(expectedPdf, result);
			mockPdfRenderer.Verify(r => r.RenderDocument(It.IsAny<DocumentModel>()), Times.Once);
		}
	}
}
