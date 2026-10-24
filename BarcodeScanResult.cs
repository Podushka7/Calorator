namespace NutritionApp.BarcodeRecognition;

public enum BarcodeScanStatus
{
    NoCode,
    ProductNotFound,
    Found
}

/// <summary>Результат обработки одного кадра. Сетевые ошибки не маскируются этим статусом.</summary>
public sealed record BarcodeScanResult(
    BarcodeScanStatus Status,
    DecodedBarcode? Code,
    ProductInfo? Product);
