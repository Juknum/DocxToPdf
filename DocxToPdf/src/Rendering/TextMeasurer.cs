using System;
using System.Globalization;
using PdfSharp.Drawing;
using DocxToPdf.Model;

using DocxToPdf.Constants;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Provides font creation, color parsing, and text string measurement utilities using PDFsharp XGraphics.
	/// </summary>
	public class TextMeasurer : ITextMeasurer {
		/// <inheritdoc />
		XFont ITextMeasurer.CreateFont(RunModel run) => CreateFont(run);

		/// <inheritdoc />
		XColor ITextMeasurer.ParseColor(string? hex, XColor defaultColor) => ParseColor(hex, defaultColor);

		/// <inheritdoc />
		XSize ITextMeasurer.MeasureString(XGraphics gfx, string text, XFont font) => MeasureString(gfx, text, font);

		/// <inheritdoc />
		public static XColor ParseColor(string? hexColor, XColor defaultColor) {
			if (string.IsNullOrWhiteSpace(hexColor) || string.Equals(hexColor, ColorConstants.Auto, StringComparison.OrdinalIgnoreCase)) {
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

		/// <inheritdoc />
		public static XFont CreateFont(RunModel run) {
			if (run == null) throw new ArgumentNullException(nameof(run));

			string family = string.IsNullOrWhiteSpace(run.FontFamily) ? FontConstants.DefaultFontFamily : run.FontFamily;
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
				return new XFont(FontConstants.DefaultFontFamily, size, style);
			}
		}

		/// <summary>
		/// Creates an <see cref="XFont"/> instance from font family name, size in points, and style flags.
		/// </summary>
		/// <param name="familyName">Font family name.</param>
		/// <param name="sizePt">Font size in points.</param>
		/// <param name="isBold">Whether font is bold.</param>
		/// <param name="isItalic">Whether font is italic.</param>
		/// <returns>An <see cref="XFont"/> instance.</returns>
		public static XFont CreateFont(string familyName, double sizePt, bool isBold = false, bool isItalic = false) {
			string family = string.IsNullOrWhiteSpace(familyName) ? FontConstants.DefaultFontFamily : familyName;
			double size = sizePt > 0 ? sizePt : 11.0;

			XFontStyleEx style = XFontStyleEx.Regular;
			if (isBold && isItalic) style = XFontStyleEx.BoldItalic;
			else if (isBold) style = XFontStyleEx.Bold;
			else if (isItalic) style = XFontStyleEx.Italic;

			try {
				return new XFont(family, size, style);
			} catch {
				return new XFont(FontConstants.DefaultFontFamily, size, style);
			}
		}

		/// <inheritdoc />
		public static XSize MeasureString(XGraphics gfx, string text, XFont font) {
			if (gfx == null) throw new ArgumentNullException(nameof(gfx));
			if (font == null) throw new ArgumentNullException(nameof(font));
			if (string.IsNullOrEmpty(text)) return new XSize(0, font.Height);
			return gfx.MeasureString(text, font);
		}
	}
}
