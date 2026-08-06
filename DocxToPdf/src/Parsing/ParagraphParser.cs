using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxToPdf.Model;

namespace DocxToPdf.Parsing {
	public class ParagraphParser {
		private readonly StyleResolver _styleResolver;
		private readonly NumberingResolver _numberingResolver;

		public ParagraphParser(StyleResolver styleResolver, NumberingResolver numberingResolver) {
			_styleResolver = styleResolver;
			_numberingResolver = numberingResolver;
		}

		public ParagraphModel ParseParagraph(Paragraph p, MediaResolver mediaResolver) {
			ParagraphModel pModel = new ParagraphModel();

			ParagraphProperties? pPr = p.ParagraphProperties;
			string? paragraphStyleId = pPr?.ParagraphStyleId?.Val?.Value;

			// Resolve paragraph styles (alignment, spacing, indents)
			ResolvedParagraphStyle resolvedPStyle = _styleResolver.ResolveParagraphStyle(pPr, paragraphStyleId);
			pModel.Alignment = resolvedPStyle.Alignment;
			pModel.SpacingBeforePt = resolvedPStyle.SpacingBeforePt;
			pModel.SpacingAfterPt = resolvedPStyle.SpacingAfterPt;
			pModel.LineHeightPt = resolvedPStyle.LineHeightPt;
			pModel.IsLineHeightMultiple = resolvedPStyle.IsLineHeightMultiple;
			pModel.LeftIndentPt = resolvedPStyle.LeftIndentPt;
			pModel.RightIndentPt = resolvedPStyle.RightIndentPt;
			pModel.FirstLineIndentPt = resolvedPStyle.FirstLineIndentPt;
			pModel.HangingIndentPt = resolvedPStyle.HangingIndentPt;

			// Resolve List & Bullet formatting (w:numPr)
			if (pPr?.NumberingProperties != null) {
				pModel.ListFormat = _numberingResolver.ResolveListFormat(pPr.NumberingProperties);
			}

			// Iterate over child elements (Runs, SimpleFields, Drawings, Hyperlinks)
			foreach (var child in p.ChildElements) {
				if (child is Run r) {
					ParseRun(r, pPr, paragraphStyleId, pModel, mediaResolver);
				} else if (child is SimpleField simpleField) {
					ParseSimpleField(simpleField, pPr, paragraphStyleId, pModel);
				} else if (child is Hyperlink hyperlink) {
					foreach (var run in hyperlink.Elements<Run>()) {
						ParseRun(run, pPr, paragraphStyleId, pModel, mediaResolver, isHyperlink: true);
					}
				} else if (child is SdtRun sdtRun) {
					foreach (var run in sdtRun.Descendants<Run>()) {
						ParseRun(run, pPr, paragraphStyleId, pModel, mediaResolver);
					}
				}
			}

			return pModel;
		}

		public List<IBlockElement> ParseParagraphToElements(Paragraph p, MediaResolver mediaResolver, TableParser? tableParser = null) {
			List<IBlockElement> elements = new List<IBlockElement>();

			// Extract drawings and pictures embedded in paragraph
			List<DrawingModel> drawings = ExtractAllDrawings(p, mediaResolver);
			elements.AddRange(drawings);

			// Parse paragraph text and formatting
			ParagraphModel pModel = ParseParagraph(p, mediaResolver);
			elements.Add(pModel);

			return elements;
		}


		private List<DrawingModel> ExtractAllDrawings(OpenXmlElement container, MediaResolver mediaResolver) {
			List<DrawingModel> drawings = new List<DrawingModel>();
			HashSet<string> seenRelIds = new HashSet<string>();

			foreach (AlternateContent altContent in container.Descendants<AlternateContent>()) {
				AlternateContentChoice? choice = altContent.GetFirstChild<AlternateContentChoice>();
				if (choice != null) {
					foreach (Drawing drw in choice.Descendants<Drawing>()) {
						DrawingModel? model = mediaResolver.ExtractDrawing(drw);
						if (model != null) {
							ExtractTextboxParagraphs(drw, model, mediaResolver);
							drawings.Add(model);
							if (!string.IsNullOrEmpty(model.RelationshipId)) seenRelIds.Add(model.RelationshipId);
						}
					}
				}
			}

			foreach (Drawing drawing in container.Descendants<Drawing>()) {
				if (drawing.Ancestors<AlternateContent>().Any()) continue;
				DrawingModel? model = mediaResolver.ExtractDrawing(drawing);
				if (model != null) {
					if (model.Placement == DrawingPlacement.Inline && !string.IsNullOrEmpty(model.RelationshipId) && seenRelIds.Contains(model.RelationshipId)) {
						continue;
					}
					ExtractTextboxParagraphs(drawing, model, mediaResolver);
					drawings.Add(model);
					if (!string.IsNullOrEmpty(model.RelationshipId)) seenRelIds.Add(model.RelationshipId);
				}
			}

			foreach (Picture pict in container.Descendants<Picture>()) {
				if (pict.Ancestors<AlternateContent>().Any()) continue;
				DrawingModel? model = mediaResolver.ExtractPict(pict);
				if (model != null) {
					if (string.IsNullOrEmpty(model.RelationshipId) || seenRelIds.Add(model.RelationshipId)) {
						ExtractTextboxParagraphs(pict, model, mediaResolver);
						drawings.Add(model);
					}
				}
			}

			return drawings;
		}

		private void ExtractTextboxParagraphs(OpenXmlElement container, DrawingModel model, MediaResolver mediaResolver) {
			foreach (Paragraph txbxP in container.Descendants<Paragraph>()) {
				ParagraphModel txbxPModel = ParseParagraph(txbxP, mediaResolver);
				if (txbxPModel.Runs.Count > 0) {
					model.TextboxParagraphs.Add(txbxPModel);
				}
			}
		}

		private void ParseRun(Run r, ParagraphProperties? pPr, string? paragraphStyleId, ParagraphModel pModel, MediaResolver mediaResolver, bool isHyperlink = false) {
			RunProperties? rPr = r.RunProperties;
			string? runStyleId = rPr?.RunStyle?.Val?.Value;

			ResolvedRunStyle resolvedRStyle = _styleResolver.ResolveRunStyle(rPr, runStyleId, pPr, paragraphStyleId);
			if (isHyperlink || (runStyleId != null && runStyleId.Equals("Hyperlink", StringComparison.OrdinalIgnoreCase))) {
				resolvedRStyle.IsUnderline = true;
				if (string.IsNullOrEmpty(resolvedRStyle.TextColorHex) || resolvedRStyle.TextColorHex == "#000000" || resolvedRStyle.TextColorHex == "000000") {
					resolvedRStyle.TextColorHex = "#0563C1";
				}
			}

			// Check for text, field codes, or inline drawings in run
			foreach (var child in r.ChildElements) {
				if (child is Text textEl) {
					RunModel runModel = CreateRunModel(textEl.Text, resolvedRStyle);
					pModel.Runs.Add(runModel);
				} else if (child is FieldCode fieldCode) {
					FieldType fieldType = ParseFieldType(fieldCode.Text);
					if (fieldType != FieldType.None) {
						RunModel runModel = CreateRunModel(string.Empty, resolvedRStyle);
						runModel.Field = fieldType;
						pModel.Runs.Add(runModel);
					}
				} else if (child is TabChar) {
					RunModel runModel = CreateRunModel("\t", resolvedRStyle);
					pModel.Runs.Add(runModel);
				} else if (child is Break br) {
					if (br.Type?.Value == BreakValues.Page) {
						pModel.HasPageBreak = true;
					} else {
						RunModel runModel = CreateRunModel("\n", resolvedRStyle);
						pModel.Runs.Add(runModel);
					}
				}
			}
		}

		private void ParseSimpleField(SimpleField simpleField, ParagraphProperties? pPr, string? paragraphStyleId, ParagraphModel pModel) {
			string instruction = simpleField.Instruction?.Value ?? string.Empty;
			FieldType fieldType = ParseFieldType(instruction);

			ResolvedRunStyle resolvedRStyle = _styleResolver.ResolveRunStyle(null, null, pPr, paragraphStyleId);

			foreach (Run r in simpleField.Elements<Run>()) {
				RunProperties? rPr = r.RunProperties;
				string? runStyleId = rPr?.RunStyle?.Val?.Value;
				resolvedRStyle = _styleResolver.ResolveRunStyle(rPr, runStyleId, pPr, paragraphStyleId);
			}

			RunModel runModel = CreateRunModel(string.Empty, resolvedRStyle);
			runModel.Field = fieldType;
			pModel.Runs.Add(runModel);
		}

		private RunModel CreateRunModel(string text, ResolvedRunStyle style) {
			return new RunModel {
				Text = text,
				FontFamily = style.FontFamily,
				FontSizePt = style.FontSizePt,
				IsBold = style.IsBold,
				IsItalic = style.IsItalic,
				IsUnderline = style.IsUnderline,
				IsStrikeThrough = style.IsStrikeThrough,
				TextColorHex = style.TextColorHex,
				BackgroundColorHex = style.BackgroundColorHex
			};
		}

		private FieldType ParseFieldType(string fieldInstruction) {
			if (string.IsNullOrWhiteSpace(fieldInstruction)) return FieldType.None;

			string trimmed = fieldInstruction.Trim();
			if (trimmed.StartsWith("PAGE", StringComparison.OrdinalIgnoreCase)) {
				return FieldType.PageNumber;
			}
			if (trimmed.StartsWith("NUMPAGES", StringComparison.OrdinalIgnoreCase)) {
				return FieldType.TotalPages;
			}
			return FieldType.None;
		}
	}
}
