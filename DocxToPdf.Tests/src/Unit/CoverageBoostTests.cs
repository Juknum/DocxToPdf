using System;
using System.IO;
using DocxToPdf.Constants;
using DocxToPdf.Model;
using DocxToPdf.Parsing;
using DocxToPdf.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Xunit;

namespace DocxToPdf.Tests.Unit {
	/// <summary>
	/// Targeted unit tests for boundary conditions, null arguments, and edge cases to ensure code coverage standards.
	/// </summary>
	public class CoverageBoostTests {
		[Fact]
		public void TestTextMeasurerColorAndFontEdgeCases() {
			var textMeasurer = new TextMeasurer();
			var iMeasurer = (ITextMeasurer)textMeasurer;

			// ParseColor variations
			Assert.Equal(XColors.Red, TextMeasurer.ParseColor(null, XColors.Red));
			Assert.Equal(XColors.Red, TextMeasurer.ParseColor("   ", XColors.Red));
			Assert.Equal(XColors.Red, TextMeasurer.ParseColor("auto", XColors.Red));
			Assert.Equal(XColors.Red, TextMeasurer.ParseColor("AUTO", XColors.Red));
			Assert.Equal(XColors.Red, TextMeasurer.ParseColor("invalid_hex", XColors.Red));
			Assert.Equal(XColors.Red, TextMeasurer.ParseColor("123", XColors.Red));

			// 6-digit Hex
			XColor c6 = TextMeasurer.ParseColor("FF0000", XColors.Black);
			Assert.Equal(255, c6.R);
			Assert.Equal(0, c6.G);

			// 8-digit ARGB Hex
			XColor c8 = TextMeasurer.ParseColor("8000FF00", XColors.Black);
			Assert.True(c8.A > 0);
			Assert.Equal(0, c8.R);
			Assert.Equal(255, c8.G);
			Assert.Equal(0, c8.B);

			// Interface delegation
			XColor cInterface = iMeasurer.ParseColor("#0000FF", XColors.Black);
			Assert.Equal(255, cInterface.B);

			// CreateFont variations
			RunModel boldItalicUnderlineStrike = new RunModel {
				FontFamily = "Arial",
				FontSizePt = 12.0,
				IsBold = true,
				IsItalic = true,
				IsUnderline = true,
				IsStrikeThrough = true
			};
			XFont font1 = TextMeasurer.CreateFont(boldItalicUnderlineStrike);
			Assert.NotNull(font1);

			RunModel italicOnly = new RunModel { IsItalic = true };
			XFont font2 = TextMeasurer.CreateFont(italicOnly);
			Assert.NotNull(font2);

			RunModel emptyRun = new RunModel { FontFamily = "" };
			XFont font3 = TextMeasurer.CreateFont(emptyRun);
			Assert.NotNull(font3);

			XFont font4 = TextMeasurer.CreateFont("Times New Roman", 14.0, isBold: true, isItalic: false);
			Assert.NotNull(font4);

			XFont font5 = TextMeasurer.CreateFont("", 0.0, isBold: true, isItalic: true);
			Assert.NotNull(font5);

			XFont fontInterface = iMeasurer.CreateFont(boldItalicUnderlineStrike);
			Assert.NotNull(fontInterface);

			// MeasureString empty text
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);
			XSize sizeEmpty = TextMeasurer.MeasureString(gfx, "", font1);
			Assert.Equal(0, sizeEmpty.Width);

			XSize sizeInterface = iMeasurer.MeasureString(gfx, "Hello", font1);
			Assert.True(sizeInterface.Width > 0);

			Assert.Throws<ArgumentNullException>(() => TextMeasurer.CreateFont((RunModel)null!));
			Assert.Throws<ArgumentNullException>(() => TextMeasurer.MeasureString(null!, "Text", font1));
			Assert.Throws<ArgumentNullException>(() => TextMeasurer.MeasureString(gfx, "Text", null!));
		}

		[Fact]
		public void TestHeaderFooterRendererEdgeCases() {
			var renderer = new HeaderFooterRenderer();
			var iRenderer = (IHeaderFooterRenderer)renderer;
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			SectionModel emptySection = new SectionModel();
			HeaderFooterRenderer.RenderHeader(emptySection, 1, 1, gfx);
			HeaderFooterRenderer.RenderFooter(emptySection, 1, 1, gfx);

			// Section with First, Even, Default Header/Footer
			SectionModel fullSection = new SectionModel();
			fullSection.HeaderFirst = new HeaderFooterModel();
			fullSection.HeaderFirst.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "First Header" } } });

			fullSection.HeaderEven = new HeaderFooterModel();
			fullSection.HeaderEven.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Even Header" } } });

			fullSection.HeaderDefault = new HeaderFooterModel();
			fullSection.HeaderDefault.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Default Header" } } });
			fullSection.HeaderDefault.Elements.Add(new TableModel());
			fullSection.HeaderDefault.Elements.Add(new DrawingModel());

			fullSection.FooterFirst = new HeaderFooterModel();
			fullSection.FooterFirst.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "First Footer" } } });

			fullSection.FooterEven = new HeaderFooterModel();
			fullSection.FooterEven.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Even Footer" } } });

			fullSection.FooterDefault = new HeaderFooterModel();
			fullSection.FooterDefault.Elements.Add(new ParagraphModel { Runs = { new RunModel { Text = "Default Footer" } } });
			fullSection.FooterDefault.Elements.Add(new TableModel());
			fullSection.FooterDefault.Elements.Add(new DrawingModel());

			// Test Page 1 (First)
			HeaderFooterRenderer.RenderHeader(fullSection, 1, 3, gfx);
			HeaderFooterRenderer.RenderFooter(fullSection, 1, 3, gfx);

			// Test Page 2 (Even)
			HeaderFooterRenderer.RenderHeader(fullSection, 2, 3, gfx);
			HeaderFooterRenderer.RenderFooter(fullSection, 2, 3, gfx);

			// Test Page 3 (Default)
			HeaderFooterRenderer.RenderHeader(fullSection, 3, 3, gfx);
			HeaderFooterRenderer.RenderFooter(fullSection, 3, 3, gfx);

			// Test Interface Delegation
			iRenderer.RenderHeader(fullSection, 1, 3, gfx);
			iRenderer.RenderFooter(fullSection, 1, 3, gfx);

			Assert.Throws<ArgumentNullException>(() => HeaderFooterRenderer.RenderHeader(null!, 1, 1, gfx));
			Assert.Throws<ArgumentNullException>(() => HeaderFooterRenderer.RenderHeader(fullSection, 1, 1, null!));
			Assert.Throws<ArgumentNullException>(() => HeaderFooterRenderer.RenderFooter(null!, 1, 1, gfx));
			Assert.Throws<ArgumentNullException>(() => HeaderFooterRenderer.RenderFooter(fullSection, 1, 1, null!));
		}

		[Fact]
		public void TestImageRendererEdgeCases() {
			var renderer = new ImageRenderer();
			var iRenderer = (IImageRenderer)renderer;
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			// MeasureDrawing null check
			Assert.Throws<ArgumentNullException>(() => ImageRenderer.MeasureDrawing(null!, 500));

			// Inline scaling
			DrawingModel inlineLarge = new DrawingModel {
				Placement = DrawingPlacement.Inline,
				WidthPt = 600,
				HeightPt = 300
			};
			var (w, h) = ImageRenderer.MeasureDrawing(inlineLarge, 500);
			Assert.Equal(500, w);
			Assert.Equal(250, h);

			// Render shape with fill color and border color
			DrawingModel shapeDrawing = new DrawingModel {
				Placement = DrawingPlacement.Inline,
				WidthPt = 100,
				HeightPt = 50,
				FillColorHex = "#FF0000",
				BorderColorHex = "#000000"
			};
			shapeDrawing.TextboxParagraphs.Add(new ParagraphModel { Runs = { new RunModel { Text = "Inside Textbox" } } });

			double currentY = 50;
			double consumed = ImageRenderer.RenderDrawing(shapeDrawing, gfx, 50, ref currentY, 500);
			Assert.True(consumed > 0);

			// Positioning calculation tests
			DrawingModel floatingPage = new DrawingModel {
				Placement = DrawingPlacement.Floating,
				HorizontalRelativeFrom = "page",
				VerticalRelativeFrom = "page",
				AlignH = "center",
				AlignV = "center",
				OffsetXPt = 10,
				OffsetYPt = 20
			};
			double posX = ImageRenderer.CalculateX(floatingPage, 50, 500, 100);
			double posY = ImageRenderer.CalculateY(floatingPage, 50, 100);
			Assert.True(posX > 0);
			Assert.True(posY > 0);

			DrawingModel floatingMargin = new DrawingModel {
				Placement = DrawingPlacement.Floating,
				HorizontalRelativeFrom = "margin",
				VerticalRelativeFrom = "margin",
				AlignH = "right",
				AlignV = "bottom",
				OffsetXPt = 5,
				OffsetYPt = 5
			};
			double posXMargin = ImageRenderer.CalculateX(floatingMargin, 50, 500, 100);
			double posYMargin = ImageRenderer.CalculateY(floatingMargin, 50, 100);
			Assert.True(posXMargin > 0);
			Assert.True(posYMargin > 0);

			// Interface delegation
			iRenderer.MeasureDrawing(inlineLarge, 500);
			double dummyY = 10;
			iRenderer.RenderDrawing(shapeDrawing, gfx, 10, ref dummyY, 500);
		}

		[Fact]
		public void TestEmfRasterizerNullAndInvalidData() {
			using PdfDocument pdf = new PdfDocument();
			PdfPage page = pdf.AddPage();
			using XGraphics gfx = XGraphics.FromPdfPage(page);

			Assert.False(EmfRasterizer.RenderEmf(null!, gfx, 0, 0, 100, 100));
			Assert.False(EmfRasterizer.RenderEmf(Array.Empty<byte>(), gfx, 0, 0, 100, 100));
			Assert.False(EmfRasterizer.RenderEmf(new byte[] { 0x00, 0x01, 0x02, 0x03 }, gfx, 0, 0, 100, 100));
		}
	}
}
