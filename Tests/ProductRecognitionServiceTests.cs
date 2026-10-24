using System.Drawing;
using Xunit;

namespace NutritionApp.BarcodeRecognition.Tests;

public sealed class ProductRecognitionServiceTests
{
    private const string Ean13 = "4640009499755";
    private const string Gtin14 = "04640009499755";
    private const string ProductJson = """
        {
          "Name": "Творог",
          "Barcode": "04640009499755",
          "CaloriesPer100g": 121,
          "ProteinPer100g": 48,
          "FatPer100g": 15,
          "CarbohydratesPer100g": 9
        }
        """;

    [Fact]
    public void Constructor_rejects_null_one_c_client()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProductRecognitionService(null!, new NormalizedProductJsonParser(), new StubImageDecoder(null)));
    }

    [Fact]
    public void Constructor_rejects_null_response_parser()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProductRecognitionService(new StubProductClient(ProductJson), null!, new StubImageDecoder(null)));
    }

    [Fact]
    public void Constructor_rejects_null_image_decoder()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProductRecognitionService(new StubProductClient(ProductJson), new NormalizedProductJsonParser(), null!));
    }

    [Fact]
    public async Task RecognizeProductAsync_normalizes_ean13_and_parses_product()
    {
        var client = new StubProductClient(ProductJson);
        var service = CreateService(client, new StubImageDecoder(null));

        ProductInfo product = Assert.IsType<ProductInfo>(await service.RecognizeProductAsync(Ean13));

        Assert.Equal(Gtin14, client.LastBarcode);
        Assert.Equal("Творог", product.Name);
        Assert.Equal(121, product.CaloriesPer100g);
    }

    [Fact]
    public async Task RecognizeProductAsync_rejects_invalid_barcode_before_http_request()
    {
        var client = new StubProductClient(ProductJson);
        var service = CreateService(client, new StubImageDecoder(null));

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecognizeProductAsync("123"));

        Assert.Null(client.LastBarcode);
    }

    [Fact]
    public async Task RecognizeProductAsync_returns_null_when_client_reports_not_found()
    {
        var client = new StubProductClient(null);
        var service = CreateService(client, new StubImageDecoder(null));

        Assert.Null(await service.RecognizeProductAsync(Ean13));
        Assert.Equal(Gtin14, client.LastBarcode);
    }

    [Fact]
    public async Task RecognizeProductAsync_propagates_cancellation_after_client_response()
    {
        var client = new StubProductClient(ProductJson);
        var service = CreateService(client, new StubImageDecoder(null));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RecognizeProductAsync(Ean13, cancellation.Token));

        Assert.Equal(Gtin14, client.LastBarcode);
    }

    [Fact]
    public async Task RecognizeProductAsync_propagates_invalid_server_json()
    {
        var client = new StubProductClient("not-json");
        var service = CreateService(client, new StubImageDecoder(null));

        await Assert.ThrowsAsync<InvalidDataException>(() => service.RecognizeProductAsync(Ean13));
    }

    [Fact]
    public async Task RecognizeFromImageAsync_returns_no_code_without_calling_one_c()
    {
        var client = new StubProductClient(ProductJson);
        var service = CreateService(client, new StubImageDecoder(null));
        using var frame = new Bitmap(2, 2);

        BarcodeScanResult result = await service.RecognizeFromImageAsync(frame);

        Assert.Equal(BarcodeScanStatus.NoCode, result.Status);
        Assert.Null(result.Code);
        Assert.Null(result.Product);
        Assert.Null(client.LastBarcode);
    }

    [Fact]
    public async Task RecognizeFromImageAsync_returns_found_product_for_decoded_code()
    {
        var client = new StubProductClient(ProductJson);
        var decoder = new StubImageDecoder(new DecodedBarcode(Gtin14, BarcodeKind.Ean13));
        var service = CreateService(client, decoder);
        using var frame = new Bitmap(2, 2);

        BarcodeScanResult result = await service.RecognizeFromImageAsync(frame);

        Assert.Equal(BarcodeScanStatus.Found, result.Status);
        Assert.Equal(Gtin14, result.Code?.Gtin14);
        Assert.Equal("Творог", result.Product?.Name);
        Assert.Equal(Gtin14, client.LastBarcode);
    }

    [Fact]
    public async Task RecognizeFromImageAsync_returns_product_not_found_for_unknown_code()
    {
        var client = new StubProductClient(null);
        var decoder = new StubImageDecoder(new DecodedBarcode(Gtin14, BarcodeKind.Gs1DataMatrix));
        var service = CreateService(client, decoder);
        using var frame = new Bitmap(2, 2);

        BarcodeScanResult result = await service.RecognizeFromImageAsync(frame);

        Assert.Equal(BarcodeScanStatus.ProductNotFound, result.Status);
        Assert.Equal(BarcodeKind.Gs1DataMatrix, result.Code?.Kind);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task RecognizeFromImageAsync_rejects_null_frame()
    {
        var service = CreateService(new StubProductClient(ProductJson), new StubImageDecoder(null));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.RecognizeFromImageAsync(null!));
    }

    [Fact]
    public async Task RecognizeFromImageAsync_honors_cancellation_before_decoding()
    {
        var client = new StubProductClient(ProductJson);
        var decoder = new StubImageDecoder(null);
        var service = CreateService(client, decoder);
        using var frame = new Bitmap(2, 2);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RecognizeFromImageAsync(frame, cancellation.Token));

        Assert.Equal(0, decoder.Calls);
        Assert.Null(client.LastBarcode);
    }

    [Fact]
    public async Task RecognizeFromImageAsync_does_not_swallow_decoder_exception()
    {
        var service = CreateService(new StubProductClient(ProductJson), new ThrowingImageDecoder());
        using var frame = new Bitmap(2, 2);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecognizeFromImageAsync(frame));
    }

    private static ProductRecognitionService CreateService(
        IOneCProductClient client,
        IBarcodeImageDecoder decoder) =>
        new(client, new NormalizedProductJsonParser(), decoder);

    private sealed class StubProductClient(string? response) : IOneCProductClient
    {
        public string? LastBarcode { get; private set; }

        public Task<string?> GetProductDataAsync(
            string barcode,
            CancellationToken cancellationToken = default)
        {
            LastBarcode = barcode;
            return Task.FromResult(response);
        }
    }

    private sealed class StubImageDecoder(DecodedBarcode? decoded) : IBarcodeImageDecoder
    {
        public int Calls { get; private set; }

        public DecodedBarcode? Decode(Bitmap frame)
        {
            Calls++;
            return decoded;
        }
    }

    private sealed class ThrowingImageDecoder : IBarcodeImageDecoder
    {
        public DecodedBarcode? Decode(Bitmap frame) =>
            throw new InvalidOperationException("Decoder failure.");
    }
}
