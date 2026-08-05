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

			// Iterate over child elements (Runs, SimpleFields, Drawings)
			foreach (var child in p.ChildElements) {
				if (child is Run r) {
					ParseRun(r, pPr, paragraphStyleId, pModel, mediaResolver);
				} else if (child is SimpleField simpleField) {
					ParseSimpleField(simpleField, pPr, paragraphStyleId, pModel);
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
			if (pModel.Runs.Count > 0 || pModel.ListFormat != null || elements.Count == 0) {
				elements.Add(pModel);
			}

			return elements;
		}


		private List<DrawingModel> ExtractAllDrawings(OpenXmlElement container, MediaResolver mediaResolver) {
			List<DrawingModel> drawings = new List<DrawingModel>();
			HashSet<string> seenRelIds = new HashSet<string>();

			foreach (Drawing drawing in container.Descendants<Drawing>()) {
				DrawingModel? model = mediaResolver.ExtractDrawing(drawing);
				if (model != null && !string.IsNullOrEmpty(model.RelationshipId)) {
					if (seenRelIds.Add(model.RelationshipId)) {
						drawings.Add(model);
					}
				}
			}

			foreach (Picture pict in container.Descendants<Picture>()) {
				DrawingModel? model = mediaResolver.ExtractPict(pict);
				if (model != null && !string.IsNullOrEmpty(model.RelationshipId)) {
					if (seenRelIds.Add(model.RelationshipId)) {
						drawings.Add(model);
					}
				}
			}

			return drawings;
		}

		private void ParseRun(Run r, ParagraphProperties? pPr, string? paragraphStyleId, ParagraphModel pModel, MediaResolver mediaResolver) {
			RunProperties? rPr = r.RunProperties;
			string? runStyleId = rPr?.RunStyle?.Val?.Value;

			ResolvedRunStyle resolvedRStyle = _styleResolver.ResolveRunStyle(rPr, runStyleId, pPr, paragraphStyleId);

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
					RunModel runModel = CreateRunModel("\n", resolvedRStyle);
					pModel.Runs.Add(runModel);
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
