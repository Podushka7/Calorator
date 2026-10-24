using Xunit;

namespace NutritionApp.BarcodeRecognition.Tests;

public sealed class GtinBarcodeValidatorTests
{
    [Theory]
    [InlineData("96385074", "00000096385074")]
    [InlineData("036000291452", "00036000291452")]
    [InlineData("4006381333931", "04006381333931")]
    [InlineData("04640009499755", "04640009499755")]
    public void ToGtin14_converts_supported_valid_codes(string input, string expected)
    {
        Assert.True(GtinBarcodeValidator.IsValid(input));
        Assert.Equal(expected, GtinBarcodeValidator.ToGtin14(input));
    }

    [Fact]
    public void ToGtin14_trims_outer_whitespace_and_preserves_leading_zeroes()
    {
        Assert.Equal("04640009499755", GtinBarcodeValidator.ToGtin14("  4640009499755  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("4006381333932")]
    [InlineData("400638133393A")]
    [InlineData("123456789")]
    public void IsValid_rejects_empty_malformed_or_bad_check_digit_codes(string? barcode)
    {
        Assert.False(GtinBarcodeValidator.IsValid(barcode));
    }

    [Fact]
    public void ToGtin14_throws_for_invalid_code()
    {
        Assert.Throws<ArgumentException>(() => GtinBarcodeValidator.ToGtin14("4006381333932"));
    }
}
