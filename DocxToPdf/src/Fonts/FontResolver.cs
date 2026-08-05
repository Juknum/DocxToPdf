using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using PdfSharp.Fonts;

namespace DocxToPdf.Fonts {
	/// <summary>
	/// Cross-platform implementation of PDFsharp's <see cref="IFontResolver"/>.
	/// Scans macOS, Linux, and Windows system directories for TrueType/OpenType fonts
	/// with support for standard fonts (Arial, Calibri, Times New Roman, etc.) and automatic fallback.
	/// </summary>
	public class CrossPlatformFontResolver : IFontResolver {
		private static readonly Lazy<CrossPlatformFontResolver> LazyInstance = new(() => new CrossPlatformFontResolver());
		public static CrossPlatformFontResolver Instance => LazyInstance.Value;

		private static readonly ConcurrentDictionary<string, byte[]> FontDataCache = new(StringComparer.OrdinalIgnoreCase);
		private static readonly ConcurrentDictionary<string, string> FaceToPathMap = new(StringComparer.OrdinalIgnoreCase);
		private static readonly List<string> SystemFontDirectories = new();

		private static bool _initialized;
#if NET9_0_OR_GREATER
		private static readonly System.Threading.Lock InitLock = new();
#else
		private static readonly object InitLock = new();
#endif

		public CrossPlatformFontResolver() {
			EnsureInitialized();
		}

		/// <summary>
		/// Registers this font resolver globally with PDFsharp if not already registered.
		/// </summary>
		public static void Register() {
			if (GlobalFontSettings.FontResolver is not CrossPlatformFontResolver) {
				GlobalFontSettings.FontResolver = Instance;
			}
		}

		private static void EnsureInitialized() {
			if (_initialized) return;
			lock (InitLock) {
				if (_initialized) return;

				PopulateFontDirectories();
				_initialized = true;
			}
		}

		private static void PopulateFontDirectories() {
			SystemFontDirectories.Clear();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				AddDirectoryIfExists("/System/Library/Fonts");
				AddDirectoryIfExists("/System/Library/Fonts/Supplemental");
				AddDirectoryIfExists("/Library/Fonts");
				string userFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Fonts");
				AddDirectoryIfExists(userFonts);
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				string winFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
				AddDirectoryIfExists(winFonts);
			} else {
				// Linux and Unix-like OS
				AddDirectoryIfExists("/usr/share/fonts");
				AddDirectoryIfExists("/usr/local/share/fonts");
				string userFonts1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fonts");
				string userFonts2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "fonts");
				AddDirectoryIfExists(userFonts1);
				AddDirectoryIfExists(userFonts2);
			}
		}

		private static void AddDirectoryIfExists(string path) {
			if (Directory.Exists(path) && !SystemFontDirectories.Contains(path, StringComparer.OrdinalIgnoreCase)) {
				SystemFontDirectories.Add(path);
			}
		}

		public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) {
			EnsureInitialized();

			string fontKey = BuildFontKey(familyName, isBold, isItalic);

			// 1. Check if face is already mapped
			if (FaceToPathMap.ContainsKey(fontKey)) {
				return new FontResolverInfo(fontKey);
			}

			// 2. Search for explicit file match
			string? filePath = FindFontFile(familyName, isBold, isItalic);

			// 3. Fallback to style variations if exact variant isn't found
			if (filePath == null && (isBold || isItalic)) {
				filePath = FindFontFile(familyName, false, false);
			}

			// 4. Map font family aliases (e.g. Calibri -> Carlito/Arial, Helvetica -> Arial, etc.)
			if (filePath == null) {
				filePath = FindFontAlias(familyName, isBold, isItalic);
			}

			// 5. Ultimate fallback: find any valid TTF font on system so rendering never crashes
			if (filePath == null) {
				filePath = FindFallbackFont();
			}

			if (filePath != null) {
				FaceToPathMap[fontKey] = filePath;
				return new FontResolverInfo(fontKey);
			}

			return null;
		}

		public byte[]? GetFont(string faceName) {
			EnsureInitialized();

			if (FontDataCache.TryGetValue(faceName, out byte[]? cachedBytes)) {
				return cachedBytes;
			}

			if (FaceToPathMap.TryGetValue(faceName, out string? filePath) && File.Exists(filePath)) {
				try {
					byte[] bytes = File.ReadAllBytes(filePath);
					FontDataCache[faceName] = bytes;
					return bytes;
				} catch {
					// Read failure fallback
				}
			}

			return null;
		}

		private static string BuildFontKey(string familyName, bool isBold, bool isItalic) {
			string style = (isBold, isItalic) switch {
				(true, true) => "BoldItalic",
				(true, false) => "Bold",
				(false, true) => "Italic",
				(false, false) => "Regular"
			};
			return $"{familyName.Trim().ToLowerInvariant()}#{style}";
		}

		private static string? FindFontFile(string familyName, bool isBold, bool isItalic) {
			string cleanFamily = familyName.Trim().ToLowerInvariant().Replace(" ", "");
			List<string> candidateNames = GetCandidateFilenames(cleanFamily, isBold, isItalic);

			foreach (string dir in SystemFontDirectories) {
				foreach (string candidate in candidateNames) {
					string path = Path.Combine(dir, candidate);
					if (File.Exists(path)) return path;

					// Case-insensitive file search in directory
					try {
						string[] matches = Directory.GetFiles(dir, candidate, SearchOption.AllDirectories);
						if (matches.Length > 0) return matches[0];
					} catch {
						// Permission or path errors ignored
					}
				}
			}

			return null;
		}

		private static List<string> GetCandidateFilenames(string cleanFamilyName, bool isBold, bool isItalic) {
			List<string> list = new();
			string suffix = (isBold, isItalic) switch {
				(true, true) => "bi",
				(true, false) => "b",
				(false, true) => "i",
				(false, false) => ""
			};

			string fullSuffix = (isBold, isItalic) switch {
				(true, true) => " Bold Italic",
				(true, false) => " Bold",
				(false, true) => " Italic",
				(false, false) => " Regular"
			};

			// Common naming patterns
			list.Add($"{cleanFamilyName}{suffix}.ttf");
			list.Add($"{cleanFamilyName}{suffix}.otf");
			list.Add($"{cleanFamilyName}{fullSuffix}.ttf");
			list.Add($"{cleanFamilyName}{fullSuffix}.otf");
			list.Add($"{cleanFamilyName}.ttf");
			list.Add($"{cleanFamilyName}.otf");
			list.Add($"{cleanFamilyName}.ttc");

			// Family specific naming conventions
			if (cleanFamilyName == "arial") {
				if (isBold && isItalic) list.Add("arialbi.ttf");
				else if (isBold) list.Add("arialbd.ttf");
				else if (isItalic) list.Add("ariali.ttf");
				else list.Add("arial.ttf");
			} else if (cleanFamilyName == "timesnewroman") {
				if (isBold && isItalic) list.Add("timesbi.ttf");
				else if (isBold) list.Add("timesbd.ttf");
				else if (isItalic) list.Add("timesi.ttf");
				else list.Add("times.ttf");
			} else if (cleanFamilyName == "calibri") {
				if (isBold && isItalic) list.Add("calibriz.ttf");
				else if (isBold) list.Add("calibrib.ttf");
				else if (isItalic) list.Add("calibrii.ttf");
				else list.Add("calibri.ttf");
			}

			return list;
		}

		private static string? FindFontAlias(string familyName, bool isBold, bool isItalic) {
			string clean = familyName.Trim().ToLowerInvariant();
			string aliasFamily = clean switch {
				"calibri" or "helvetica" => "arial",
				"times" or "times new roman" => "times",
				"courier" => "courier new",
				_ => "arial"
			};

			if (!string.Equals(aliasFamily, clean, StringComparison.OrdinalIgnoreCase)) {
				return FindFontFile(aliasFamily, isBold, isItalic);
			}

			return null;
		}

		private static string? FindFallbackFont() {
			foreach (string dir in SystemFontDirectories) {
				try {
					if (!Directory.Exists(dir)) continue;
					string[] ttfFiles = Directory.GetFiles(dir, "*.ttf", SearchOption.AllDirectories);
					if (ttfFiles.Length > 0) return ttfFiles[0];

					string[] otfFiles = Directory.GetFiles(dir, "*.otf", SearchOption.AllDirectories);
					if (otfFiles.Length > 0) return otfFiles[0];
				} catch {
					// Ignore access exceptions
				}
			}
			return null;
		}
	}
}
