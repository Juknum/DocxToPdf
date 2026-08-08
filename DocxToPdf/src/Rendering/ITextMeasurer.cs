using System;
using DocxToPdf.Model;
using PdfSharp.Drawing;

namespace DocxToPdf.Rendering {
	/// <summary>
	/// Interface for font creation, color parsing, and string measurement operations.
	/// </summary>
	public interface ITextMeasurer {
		/// <summary>
		/// Creates an <see cref="XFont"/> instance based on formatting properties in a <see cref="RunModel"/>.
		/// </summary>
		/// <param name="run">RunModel containing font family, size, bold, and italic attributes.</param>
		/// <returns>An <see cref="XFont"/> instance.</returns>
		XFont CreateFont(RunModel run);

		/// <summary>
		/// Parses a HEX color string into an <see cref="XColor"/>.
		/// </summary>
		/// <param name="hex">HEX color string (e.g., "#FF0000" or "00FF00").</param>
		/// <param name="defaultColor">Fallback color used if parsing fails.</param>
		/// <returns>An <see cref="XColor"/> value.</returns>
		XColor ParseColor(string? hex, XColor defaultColor);

		/// <summary>
		/// Measures bounding dimensions of a text string using the specified font and graphics context.
		/// </summary>
		/// <param name="gfx">PDFsharp graphics context.</param>
		/// <param name="text">Text string to measure.</param>
		/// <param name="font">XFont used for measurement.</param>
		/// <returns>An <see cref="XSize"/> structure containing width and height.</returns>
		XSize MeasureString(XGraphics gfx, string text, XFont font);
	}
}
