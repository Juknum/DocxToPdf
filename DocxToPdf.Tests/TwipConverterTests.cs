using DocxToPdf.Parsing;
using Xunit;

namespace DocxToPdf.Tests {
	public class TwipConverterTests {
		[Fact]
		public void TestTwipsToPoints() {
			Assert.Equal(72.0, TwipConverter.TwipsToPoints(1440));
			Assert.Equal(36.0, TwipConverter.TwipsToPoints(720));
		}

		[Fact]
		public void TestEmusToPoints() {
			Assert.Equal(72.0, TwipConverter.EmusToPoints(914400));
			Assert.Equal(10.0, TwipConverter.EmusToPoints(127000));
		}

		[Fact]
		public void TestHalfPointsToPoints() {
			Assert.Equal(12.0, TwipConverter.HalfPointsToPoints(24));
			Assert.Equal(10.5, TwipConverter.HalfPointsToPoints(21));
		}

		[Fact]
		public void TestEighthPointsToPoints() {
			Assert.Equal(1.0, TwipConverter.EighthPointsToPoints(8));
			Assert.Equal(0.5, TwipConverter.EighthPointsToPoints(4));
		}

		[Fact]
		public void TestNormalizeHexColor() {
			Assert.Equal("#FF0000", TwipConverter.NormalizeHexColor("FF0000"));
			Assert.Equal("#00FF00", TwipConverter.NormalizeHexColor("#00FF00"));
			Assert.Equal("#000000", TwipConverter.NormalizeHexColor("auto"));
			Assert.Equal("#000000", TwipConverter.NormalizeHexColor(null));
		}
	}
}
