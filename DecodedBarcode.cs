namespace NutritionApp.BarcodeRecognition;

public enum BarcodeKind
{
    Ean13,
    Gs1DataMatrix
}

/// <summary>Код, прочитанный из изображения; ключ поиска всегда GTIN-14.</summary>
public sealed record DecodedBarcode(string Gtin14, BarcodeKind Kind);
