using System.Text.Json;

namespace NutritionApp.BarcodeRecognition;

/// <summary>
/// Разбирает нормализованный JSON-объект с полями ProductInfo.
/// Для отличающегося формата 1С следует предоставить другую реализацию IProductResponseParser.
/// </summary>
public sealed class NormalizedProductJsonParser : IProductResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public ProductInfo? Parse(string response, string requestedBarcode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBarcode);

        ProductInfo product;
        try
        {
            using JsonDocument document = JsonDocument.Parse(response);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Ответ источника должен быть JSON-объектом.");
            }

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.Equals("Found", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.False)
                {
                    return null;
                }

                if (property.Value.ValueKind != JsonValueKind.True)
                {
                    throw new InvalidDataException("Поле Found должно быть логическим значением.");
                }
            }

            product = JsonSerializer.Deserialize<ProductInfo>(response, JsonOptions)
                ?? throw new InvalidDataException("Ответ источника не содержит объект продукта.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "Ответ источника не соответствует ожидаемому JSON продукта.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new InvalidDataException("В ответе отсутствует название продукта.");
        }

        string normalizedBarcode = GtinBarcodeValidator.ToGtin14(requestedBarcode);
        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            product.Barcode = normalizedBarcode;
        }
        else if (!GtinBarcodeValidator.IsValid(product.Barcode) ||
                 !string.Equals(GtinBarcodeValidator.ToGtin14(product.Barcode), normalizedBarcode, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Штрихкод продукта в ответе не совпадает с запрошенным.");
        }

        product.Name = product.Name.Trim();
        product.Barcode = normalizedBarcode;
        ValidateNutrient(product.CaloriesPer100g, nameof(product.CaloriesPer100g));
        ValidateNutrient(product.ProteinPer100g, nameof(product.ProteinPer100g));
        ValidateNutrient(product.FatPer100g, nameof(product.FatPer100g));
        ValidateNutrient(product.CarbohydratesPer100g, nameof(product.CarbohydratesPer100g));

        return product;
    }

    private static void ValidateNutrient(double? value, string propertyName)
    {
        if (value is not null && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new InvalidDataException(
                $"Поле {propertyName} должно содержать конечное неотрицательное число.");
        }
    }
}
