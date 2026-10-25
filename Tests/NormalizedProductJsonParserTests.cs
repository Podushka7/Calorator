using Xunit;

namespace NutritionApp.BarcodeRecognition.Tests;

public sealed class NormalizedProductJsonParserTests
{
    private const string ProductBarcode = "04640009499755";

    [Fact]
    public void Parse_reads_the_confirmed_one_c_product_contract()
    {
        const string json = """
            {
              "Name": "Творог",
              "Barcode": "04640009499755",
              "CaloriesPer100g": 121,
              "ProteinPer100g": 48,
              "FatPer100g": 15,
              "CarbohydratesPer100g": 9
            }
            """;

        ProductInfo product = Assert.IsType<ProductInfo>(
            new NormalizedProductJsonParser().Parse(json, ProductBarcode));

        Assert.Equal("Творог", product.Name);
        Assert.Equal(ProductBarcode, product.Barcode);
        Assert.Equal(121, product.CaloriesPer100g);
        Assert.Equal(48, product.ProteinPer100g);
        Assert.Equal(15, product.FatPer100g);
        Assert.Equal(9, product.CarbohydratesPer100g);
    }

    [Fact]
    public void Parse_returns_null_for_found_false_response()
    {
        Assert.Null(new NormalizedProductJsonParser().Parse("{\"Found\":false}", ProductBarcode));
    }

    [Fact]
    public void Parse_accepts_true_found_flag_case_insensitively()
    {
        const string json = "{\"found\":true,\"name\":\" Творог \",\"barcode\":\"4640009499755\"}";

        ProductInfo product = Assert.IsType<ProductInfo>(
            new NormalizedProductJsonParser().Parse(json, ProductBarcode));

        Assert.Equal("Творог", product.Name);
        Assert.Equal(ProductBarcode, product.Barcode);
    }

    [Theory]
    [InlineData("{\"Found\":\"false\"}")]
    [InlineData("{\"Found\":0}")]
    [InlineData("{\"Found\":null}")]
    public void Parse_rejects_non_boolean_found_flag(string json)
    {
        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"product\"")]
    [InlineData("42")]
    public void Parse_rejects_non_object_json_root(string json)
    {
        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Fact]
    public void Parse_fills_missing_barcode_from_the_valid_requested_gtin()
    {
        const string json = "{\"Name\":\"Творог\",\"CaloriesPer100g\":121}";

        ProductInfo product = Assert.IsType<ProductInfo>(
            new NormalizedProductJsonParser().Parse(json, ProductBarcode));

        Assert.Equal(ProductBarcode, product.Barcode);
        Assert.Null(product.ProteinPer100g);
    }

    [Fact]
    public void Parse_rejects_barcode_that_differs_from_the_requested_product()
    {
        const string json = "{\"Name\":\"Другой продукт\",\"Barcode\":\"04006381333931\"}";

        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Fact]
    public void Parse_rejects_negative_nutrient_values()
    {
        const string json = "{\"Name\":\"Творог\",\"ProteinPer100g\":-1}";

        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Fact]
    public void Parse_rejects_malformed_json()
    {
        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse("not-json", ProductBarcode));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_rejects_empty_response(string? response)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new NormalizedProductJsonParser().Parse(response!, ProductBarcode));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_rejects_empty_requested_barcode(string? barcode)
    {
        const string json = "{\"Name\":\"Творог\"}";

        Assert.ThrowsAny<ArgumentException>(
            () => new NormalizedProductJsonParser().Parse(json, barcode!));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Name\":\" \"}")]
    public void Parse_rejects_missing_or_blank_product_name(string json)
    {
        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Fact]
    public void Parse_rejects_invalid_requested_barcode()
    {
        Assert.Throws<ArgumentException>(
            () => new NormalizedProductJsonParser().Parse("{\"Name\":\"Творог\"}", "123"));
    }

    [Theory]
    [InlineData("04640009499756")]
    [InlineData("0464000949975A")]
    public void Parse_rejects_invalid_returned_barcode(string returnedBarcode)
    {
        string json = $"{{\"Name\":\"Творог\",\"Barcode\":\"{returnedBarcode}\"}}";

        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Theory]
    [InlineData("CaloriesPer100g")]
    [InlineData("ProteinPer100g")]
    [InlineData("FatPer100g")]
    [InlineData("CarbohydratesPer100g")]
    public void Parse_rejects_negative_value_for_each_nutrient(string propertyName)
    {
        string json = $"{{\"Name\":\"Творог\",\"{propertyName}\":-0.1}}";

        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }

    [Fact]
    public void Parse_rejects_non_finite_nutrient_value()
    {
        const string json = "{\"Name\":\"Творог\",\"CaloriesPer100g\":1e400}";

        Assert.Throws<InvalidDataException>(
            () => new NormalizedProductJsonParser().Parse(json, ProductBarcode));
    }
}
