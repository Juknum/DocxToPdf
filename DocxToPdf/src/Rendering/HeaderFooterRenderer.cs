using System;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Provides header and footer selection and rendering logic for section pages.
	/// </summary>
	public static class HeaderFooterRenderer {

		/// <summary>
		/// Renders section header elements onto the PDF canvas for the specified page.
		/// </summary>
		/// <param name="section">The section model. Cannot be null.</param>
		/// <param name="pageNumber">1-indexed page number within document.</param>
		/// <param name="totalPages">Total document page count.</param>
		/// <param name="gfx">PDFsharp graphics context. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="section"/> or <paramref name="gfx"/> is null.</exception>
		public static void RenderHeader(SectionModel section, int pageNumber, int totalPages, XGraphics gfx) {
			if (section == null) throw new ArgumentNullException(nameof(section));
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));

			HeaderFooterModel? header = SelectHeader(section, pageNumber);
			if (header == null || header.Elements.Count == 0) return;

			double left = section.PageSetup.Margins.Left;
			double top = section.PageSetup.Margins.Header;
			double width = section.PageSetup.PrintableWidth;

			double currentY = top;

			foreach (var element in header.Elements) {
				if (element is ParagraphModel paragraph) {
					var pLayout = TextLayoutEngine.MeasureParagraph(paragraph, gfx, width, pageNumber, totalPages);
					TextLayoutEngine.RenderParagraph(pLayout, gfx, left, ref currentY);
				} else if (element is TableModel table) {
					var rowLayouts = TableRenderer.MeasureTable(table, gfx, width, pageNumber, totalPages);
					foreach (var rowLayout in rowLayouts) {
						TableRenderer.RenderRow(rowLayout, table, gfx, left, currentY);
						currentY += rowLayout.Height;
					}
				} else if (element is DrawingModel drawing) {
					ImageRenderer.RenderDrawing(drawing, gfx, left, ref currentY, width);
				}
			}
		}

		/// <summary>
		/// Renders section footer elements onto the PDF canvas for the specified page.
		/// </summary>
		/// <param name="section">The section model. Cannot be null.</param>
		/// <param name="pageNumber">1-indexed page number within document.</param>
		/// <param name="totalPages">Total document page count.</param>
		/// <param name="gfx">PDFsharp graphics context. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="section"/> or <paramref name="gfx"/> is null.</exception>
		public static void RenderFooter(SectionModel section, int pageNumber, int totalPages, XGraphics gfx) {
			if (section == null) throw new ArgumentNullException(nameof(section));
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));

			HeaderFooterModel? footer = SelectFooter(section, pageNumber);
			if (footer == null || footer.Elements.Count == 0) return;

			double left = section.PageSetup.Margins.Left;
			double width = section.PageSetup.PrintableWidth;
			double bottom = section.PageSetup.Height - section.PageSetup.Margins.Footer;

			// Measure total footer height to position it cleanly near bottom margin
			double footerHeight = 0;
			foreach (var element in footer.Elements) {
				if (element is ParagraphModel paragraph) {
					var pLayout = TextLayoutEngine.MeasureParagraph(paragraph, gfx, width, pageNumber, totalPages);
					footerHeight += pLayout.TotalHeight;
				} else if (element is TableModel table) {
					var rowLayouts = TableRenderer.MeasureTable(table, gfx, width, pageNumber, totalPages);
					footerHeight += rowLayouts.Sum(r => r.Height);
				} else if (element is DrawingModel drawing) {
					var (w, h) = ImageRenderer.MeasureDrawing(drawing, width);
					footerHeight += h;
				}
			}

			double currentY = Math.Max(section.PageSetup.Height - section.PageSetup.Margins.Bottom, bottom - footerHeight);

			foreach (var element in footer.Elements) {
				if (element is ParagraphModel paragraph) {
					var pLayout = TextLayoutEngine.MeasureParagraph(paragraph, gfx, width, pageNumber, totalPages);
					TextLayoutEngine.RenderParagraph(pLayout, gfx, left, ref currentY);
				} else if (element is TableModel table) {
					var rowLayouts = TableRenderer.MeasureTable(table, gfx, width, pageNumber, totalPages);
					foreach (var rowLayout in rowLayouts) {
						TableRenderer.RenderRow(rowLayout, table, gfx, left, currentY);
						currentY += rowLayout.Height;
					}
				} else if (element is DrawingModel drawing) {
					ImageRenderer.RenderDrawing(drawing, gfx, left, ref currentY, width);
				}
			}
		}

		private static HeaderFooterModel? SelectHeader(SectionModel section, int pageNumber) {
			if (pageNumber == 1 && section.HeaderFirst != null) return section.HeaderFirst;
			if (pageNumber % 2 == 0 && section.HeaderEven != null) return section.HeaderEven;
			return section.HeaderDefault;
		}

		private static HeaderFooterModel? SelectFooter(SectionModel section, int pageNumber) {
			if (pageNumber == 1 && section.FooterFirst != null) return section.FooterFirst;
			if (pageNumber % 2 == 0 && section.FooterEven != null) return section.FooterEven;
			return section.FooterDefault;
		}
	}
}
