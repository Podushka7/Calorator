using System.Drawing;

namespace NutritionApp.BarcodeRecognition;

/// <summary>Находит продукт по GTIN через настроенный источник данных 1С.</summary>
public sealed class ProductRecognitionService : IProductRecognitionService
{
    private readonly IOneCProductClient _oneCProductClient;
    private readonly IProductResponseParser _responseParser;
    private readonly IBarcodeImageDecoder _imageDecoder;

    public ProductRecognitionService(
        IOneCProductClient oneCProductClient,
        IProductResponseParser responseParser,
        IBarcodeImageDecoder imageDecoder)
    {
        _oneCProductClient = oneCProductClient
            ?? throw new ArgumentNullException(nameof(oneCProductClient));
        _responseParser = responseParser
            ?? throw new ArgumentNullException(nameof(responseParser));
        _imageDecoder = imageDecoder
            ?? throw new ArgumentNullException(nameof(imageDecoder));
    }

    /// <inheritdoc />
    public bool ValidateBarcode(string barcode) => GtinBarcodeValidator.IsValid(barcode);

    /// <inheritdoc />
    public async Task<ProductInfo?> RecognizeProductAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateBarcode(barcode))
        {
            throw new ArgumentException(
                "Ожидается цифровой штрихкод GTIN-8, GTIN-12, GTIN-13 или GTIN-14 с корректной контрольной цифрой.",
                nameof(barcode));
        }

        string normalizedBarcode = GtinBarcodeValidator.ToGtin14(barcode);
        string? response = await _oneCProductClient
            .GetProductDataAsync(normalizedBarcode, cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (response is null)
        {
            return null;
        }

        return _responseParser.Parse(response, normalizedBarcode);
    }

    /// <inheritdoc />
    public async Task<BarcodeScanResult> RecognizeFromImageAsync(
        Bitmap frame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        cancellationToken.ThrowIfCancellationRequested();

        DecodedBarcode? decoded = _imageDecoder.Decode(frame);
        if (decoded is null)
        {
            return new BarcodeScanResult(BarcodeScanStatus.NoCode, null, null);
        }

        ProductInfo? product = await RecognizeProductAsync(decoded.Gtin14, cancellationToken)
            .ConfigureAwait(false);

        return product is null
            ? new BarcodeScanResult(BarcodeScanStatus.ProductNotFound, decoded, null)
            : new BarcodeScanResult(BarcodeScanStatus.Found, decoded, product);
    }
}
