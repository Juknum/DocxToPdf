using DocxToPdf.Fonts;
using PdfSharp.Fonts;
using Xunit;

namespace DocxToPdf.Tests {
	public class FontResolverTests {
		[Fact]
		public void TestRegisterAndInstance() {
			CrossPlatformFontResolver.Register();
			Assert.Same(CrossPlatformFontResolver.Instance, GlobalFontSettings.FontResolver);
		}

		[Fact]
		public void TestResolveTypefaceKnownFonts() {
			var resolver = CrossPlatformFontResolver.Instance;

			var infoArial = resolver.ResolveTypeface("Arial", false, false);
			Assert.NotNull(infoArial);

			var infoArialBold = resolver.ResolveTypeface("Arial", true, false);
			Assert.NotNull(infoArialBold);

			var infoArialItalic = resolver.ResolveTypeface("Arial", false, true);
			Assert.NotNull(infoArialItalic);

			var infoArialBoldItalic = resolver.ResolveTypeface("Arial", true, true);
			Assert.NotNull(infoArialBoldItalic);

			var infoCalibri = resolver.ResolveTypeface("Calibri", false, false);
			Assert.NotNull(infoCalibri);

			var infoTimes = resolver.ResolveTypeface("Times New Roman", false, false);
			Assert.NotNull(infoTimes);

			var infoCourier = resolver.ResolveTypeface("Courier", false, false);
			Assert.NotNull(infoCourier);
		}

		[Fact]
		public void TestResolveTypefaceFallback() {
			var resolver = CrossPlatformFontResolver.Instance;

			// Unknown font family should fallback to default font (e.g. Arial or system TTF fallback)
			var infoUnknown = resolver.ResolveTypeface("SomeUnknownFont123", false, false);
			Assert.NotNull(infoUnknown);

			// Resolving again should hit FaceToPathMap cache
			var infoUnknownCached = resolver.ResolveTypeface("SomeUnknownFont123", false, false);
			Assert.Equal(infoUnknown.FaceName, infoUnknownCached?.FaceName);
		}

		[Fact]
		public void TestGetFont() {
			var resolver = CrossPlatformFontResolver.Instance;
			var info = resolver.ResolveTypeface("Arial", false, false);
			Assert.NotNull(info);

			byte[]? data = resolver.GetFont(info!.FaceName);
			Assert.NotNull(data);
			Assert.True(data!.Length > 0);

			// Second call should hit FontDataCache
			byte[]? cachedData = resolver.GetFont(info.FaceName);
			Assert.Same(data, cachedData);

			// Non existent face
			byte[]? invalidData = resolver.GetFont("non_existent_face_name");
			Assert.Null(invalidData);
		}
	}
}
