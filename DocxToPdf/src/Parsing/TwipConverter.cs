using System;
using System.Globalization;

namespace DocxToPdf.Parsing {
	/// <summary>
	/// Provides unit conversion helpers (Twips, EMUs, half-points, eighth-points to PDF points) and color normalization utilities.
	/// </summary>
	public static class TwipConverter {
		/// <summary>Number of OpenXML twips per PDF point (20.0 twips/pt).</summary>
		public const double TwipsPerPoint = 20.0;

		/// <summary>Number of English Metric Units (EMUs) per PDF point (12700.0 EMUs/pt).</summary>
		public const double EmusPerPoint = 12700.0;

		/// <summary>Converts double twips to points.</summary>
		/// <param name="twips">Twips value.</param>
		/// <returns>Points value.</returns>
		public static double TwipsToPoints(double twips) => twips / TwipsPerPoint;

		/// <summary>Converts long twips to points.</summary>
		/// <param name="twips">Twips value.</param>
		/// <returns>Points value.</returns>
		public static double TwipsToPoints(long twips) => twips / TwipsPerPoint;

		/// <summary>Converts EMUs to points.</summary>
		/// <param name="emus">EMUs value.</param>
		/// <returns>Points value.</returns>
		public static double EmusToPoints(long emus) => emus / EmusPerPoint;

		/// <summary>Converts half-points to points.</summary>
		/// <param name="halfPts">Half-points value.</param>
		/// <returns>Points value.</returns>
		public static double HalfPointsToPoints(double halfPts) => halfPts / 2.0;

		/// <summary>Converts eighth-points to points.</summary>
		/// <param name="eighthPts">Eighth-points value.</param>
		/// <returns>Points value.</returns>
		public static double EighthPointsToPoints(double eighthPts) => eighthPts / 8.0;

		/// <summary>
		/// Normalizes OpenXML color hex string (e.g. "FF0000" or "auto") to standard "#RRGGBB" format.
		/// </summary>
		/// <param name="colorValue">Raw OpenXML color value string.</param>
		/// <param name="defaultHex">Fallback HEX string if colorValue is invalid or "auto".</param>
		/// <returns>Normalized "#RRGGBB" HEX color string.</returns>
		public static string NormalizeHexColor(string? colorValue, string defaultHex = "#000000") {
			if (string.IsNullOrWhiteSpace(colorValue) || colorValue!.Equals("auto", StringComparison.OrdinalIgnoreCase)) {
				return defaultHex;
			}

			string trimmed = colorValue.Trim().TrimStart('#');
			if (trimmed.Length == 6) {
				return "#" + trimmed;
			}
			if (trimmed.Length == 8) { // ARGB format, skip alpha or keep last 6
				return "#" + trimmed.Substring(2);
			}

			return defaultHex;
		}
	}
}
