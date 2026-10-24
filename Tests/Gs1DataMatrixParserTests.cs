using Xunit;

namespace NutritionApp.BarcodeRecognition.Tests;

public sealed class Gs1DataMatrixParserTests
{
    [Theory]
    [InlineData("0104640009499755")]
    [InlineData("(01)04640009499755")]
    [InlineData("]d20104640009499755")]
    [InlineData("\u001d0104640009499755")]
    [InlineData("]d2\u001d0104640009499755")]
    [InlineData("010464000949975510ABC")]
    public void TryGetGtin14_extracts_ai_01(string payload)
    {
        Assert.Equal("04640009499755", Gs1DataMatrixParser.TryGetGtin14(payload));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("]d2")]
    [InlineData("10ABC")]
    [InlineData("01046400094997")]
    [InlineData("0104640009499756")]
    [InlineData("\u001d10ABC")]
    [InlineData("(10)04640009499755")]
    [InlineData("010464000949975A")]
    public void TryGetGtin14_returns_null_for_missing_or_invalid_gtin(string? payload)
    {
        Assert.Null(Gs1DataMatrixParser.TryGetGtin14(payload));
    }
}
