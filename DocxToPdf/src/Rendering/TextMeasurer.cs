using System;
using System.Globalization;
using PdfSharp.Drawing;
using DocxToPdf.Model;

namespace DocxToPdf.Rendering {
	public static class TextMeasurer {
		public static XColor ParseColor(string? hexColor, XColor defaultColor) {
			if (string.IsNullOrWhiteSpace(hexColor) || string.Equals(hexColor, "auto", StringComparison.OrdinalIgnoreCase)) {
				return defaultColor;
			}

			string cleanHex = hexColor!.Trim().TrimStart('#');
			if (cleanHex.Length == 6 && uint.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb)) {
				byte r = (byte)((argb >> 16) & 0xFF);
				byte g = (byte)((argb >> 8) & 0xFF);
				byte b = (byte)(argb & 0xFF);
				return XColor.FromArgb(255, r, g, b);
			}

			if (cleanHex.Length == 8 && uint.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb8)) {
				byte a = (byte)((argb8 >> 24) & 0xFF);
				byte r = (byte)((argb8 >> 16) & 0xFF);
				byte g = (byte)((argb8 >> 8) & 0xFF);
				byte b = (byte)(argb8 & 0xFF);
				return XColor.FromArgb(a, r, g, b);
			}

			return defaultColor;
		}

		public static XFont CreateFont(RunModel run) {
			string family = string.IsNullOrWhiteSpace(run.FontFamily) ? "Arial" : run.FontFamily;
			double size = run.FontSizePt > 0 ? run.FontSizePt : 11.0;

			XFontStyleEx style = XFontStyleEx.Regular;
			if (run.IsBold && run.IsItalic) {
				style = XFontStyleEx.BoldItalic;
			} else if (run.IsBold) {
				style = XFontStyleEx.Bold;
			} else if (run.IsItalic) {
				style = XFontStyleEx.Italic;
			}

			if (run.IsUnderline) {
				style |= XFontStyleEx.Underline;
			}
			if (run.IsStrikeThrough) {
				style |= XFontStyleEx.Strikeout;
			}

			try {
				return new XFont(family, size, style);
			} catch {
				return new XFont("Arial", size, style);
			}
		}

		public static XFont CreateFont(string familyName, double sizePt, bool isBold = false, bool isItalic = false) {
			string family = string.IsNullOrWhiteSpace(familyName) ? "Arial" : familyName;
			double size = sizePt > 0 ? sizePt : 11.0;

			XFontStyleEx style = XFontStyleEx.Regular;
			if (isBold && isItalic) style = XFontStyleEx.BoldItalic;
			else if (isBold) style = XFontStyleEx.Bold;
			else if (isItalic) style = XFontStyleEx.Italic;

			try {
				return new XFont(family, size, style);
			} catch {
				return new XFont("Arial", size, style);
			}
		}

		public static XSize MeasureString(XGraphics gfx, string text, XFont font) {
			if (string.IsNullOrEmpty(text)) return new XSize(0, font.Height);
			return gfx.MeasureString(text, font);
		}
	}
}
