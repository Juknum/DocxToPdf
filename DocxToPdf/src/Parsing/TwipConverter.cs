using System;
using System.Globalization;

namespace DocxToPdf.Parsing {
	public static class TwipConverter {
		public const double TwipsPerPoint = 20.0;
		public const double EmusPerPoint = 12700.0;

		public static double TwipsToPoints(double twips) {
			return twips / TwipsPerPoint;
		}

		public static double TwipsToPoints(long twips) {
			return twips / TwipsPerPoint;
		}

		public static double EmusToPoints(long emus) {
			return emus / EmusPerPoint;
		}

		public static double HalfPointsToPoints(double halfPts) {
			return halfPts / 2.0;
		}

		public static double EighthPointsToPoints(double eighthPts) {
			return eighthPts / 8.0;
		}

		/// <summary>
		/// Normalizes OpenXML color hex string (e.g. "FF0000" or "auto") to standard "#RRGGBB" format.
		/// </summary>
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
