using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	public class PageContext {
		public PdfPage Page { get; set; } = null!;
		public XGraphics Graphics { get; set; } = null!;
		public SectionModel Section { get; set; } = null!;
		public int PageNumber { get; set; }
	}

	public static class PdfRenderer {

		public static PdfDocument Render(DocumentModel documentModel) {
			PdfDocument pdf = new PdfDocument();

			if (documentModel.Sections.Count == 0) {
				// Handle empty document fallback
				SectionModel defaultSection = new SectionModel();
				CreateNewPage(pdf, defaultSection, 1, out _);
				return pdf;
			}

			List<PageContext> pageContexts = new List<PageContext>();
			int globalPageCounter = 0;

			foreach (var section in documentModel.Sections) {
				globalPageCounter++;
				PageContext pageCtx = CreateNewPage(pdf, section, globalPageCounter, out XGraphics gfx);
				pageContexts.Add(pageCtx);

				double leftX = section.PageSetup.Margins.Left;
				double currentY = section.PageSetup.Margins.Top;
				double maxY = section.PageSetup.Height - section.PageSetup.Margins.Bottom;
				double printableWidth = section.PageSetup.PrintableWidth;

				foreach (var element in section.Elements) {
					if (element is ParagraphModel paragraph) {
						var pLayout = TextLayoutEngine.MeasureParagraph(paragraph, gfx, printableWidth, globalPageCounter, 1);

						// Page break check
						if (currentY + pLayout.TotalHeight > maxY && currentY > section.PageSetup.Margins.Top + 5.0) {
							globalPageCounter++;
							pageCtx = CreateNewPage(pdf, section, globalPageCounter, out gfx);
							pageContexts.Add(pageCtx);
							currentY = section.PageSetup.Margins.Top;
							pLayout = TextLayoutEngine.MeasureParagraph(paragraph, gfx, printableWidth, globalPageCounter, 1);
						}

						TextLayoutEngine.RenderParagraph(pLayout, gfx, leftX, ref currentY);

					} else if (element is TableModel table) {
						var rowLayouts = TableRenderer.MeasureTable(table, gfx, printableWidth, globalPageCounter, 1);
						var headerRows = rowLayouts.Where(r => r.IsHeader).ToList();

						foreach (var rowLayout in rowLayouts) {
							// Page break check for table row
							if (currentY + rowLayout.Height > maxY && currentY > section.PageSetup.Margins.Top + 5.0) {
								globalPageCounter++;
								pageCtx = CreateNewPage(pdf, section, globalPageCounter, out gfx);
								pageContexts.Add(pageCtx);
								currentY = section.PageSetup.Margins.Top;

								// Repeat table header rows on new page if present
								if (headerRows.Count > 0 && !rowLayout.IsHeader) {
									foreach (var hRow in headerRows) {
										TableRenderer.RenderRow(hRow, table, gfx, leftX, currentY);
										currentY += hRow.Height;
									}
								}
							}

							TableRenderer.RenderRow(rowLayout, table, gfx, leftX, currentY);
							currentY += rowLayout.Height;
						}

					} else if (element is DrawingModel drawing) {
						var (imgW, imgH) = ImageRenderer.MeasureDrawing(drawing, printableWidth);

						if (drawing.Placement == DrawingPlacement.Inline && currentY + imgH > maxY && currentY > section.PageSetup.Margins.Top + 5.0) {
							globalPageCounter++;
							pageCtx = CreateNewPage(pdf, section, globalPageCounter, out gfx);
							pageContexts.Add(pageCtx);
							currentY = section.PageSetup.Margins.Top;
						}

						ImageRenderer.RenderDrawing(drawing, gfx, leftX, ref currentY, printableWidth);
					}
				}
			}

			int totalPages = pageContexts.Count;

			// Pass 2: Render persistent Headers and Footers with exact total page count
			foreach (var pageCtx in pageContexts) {
				HeaderFooterRenderer.RenderHeader(pageCtx.Section, pageCtx.PageNumber, totalPages, pageCtx.Graphics);
				HeaderFooterRenderer.RenderFooter(pageCtx.Section, pageCtx.PageNumber, totalPages, pageCtx.Graphics);
			}

			return pdf;
		}

		private static PageContext CreateNewPage(PdfDocument pdf, SectionModel section, int pageNumber, out XGraphics gfx) {
			PdfPage page = pdf.AddPage();
			page.Width = XUnit.FromPoint(section.PageSetup.Width);
			page.Height = XUnit.FromPoint(section.PageSetup.Height);

			if (section.PageSetup.Orientation == DocxToPdf.Model.PageOrientation.Landscape) {
				page.Orientation = PdfSharp.PageOrientation.Landscape;
			} else {
				page.Orientation = PdfSharp.PageOrientation.Portrait;
			}

			gfx = XGraphics.FromPdfPage(page);
			gfx.DrawRectangle(XBrushes.White, 0, 0, page.Width, page.Height);

			return new PageContext {
				Page = page,
				Graphics = gfx,
				Section = section,
				PageNumber = pageNumber
			};
		}
	}
}
