namespace NutritionApp.BarcodeRecognition;

/// <summary>Извлекает только GTIN из AI (01), не проверяет маркировку и подлинность товара.</summary>
public static class Gs1DataMatrixParser
{
    public static string? TryGetGtin14(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return null;
        }

        string value = payload.StartsWith("]d2", StringComparison.Ordinal)
            ? payload[3..]
            : payload;

        // ZXing returns a leading GS (ASCII 29) for the first FNC1 in a GS1 Data Matrix.
        // Some other readers include the AIM symbology identifier instead; both forms are accepted.
        if (value.Length > 0 && value[0] == '\u001d')
        {
            value = value[1..];
        }

        if (value.StartsWith("(01)", StringComparison.Ordinal))
        {
            value = value[4..];
        }
        else if (value.StartsWith("01", StringComparison.Ordinal))
        {
            value = value[2..];
        }
        else
        {
            return null;
        }

        if (value.Length < 14)
        {
            return null;
        }

        string gtin14 = value[..14];
        return GtinBarcodeValidator.IsValid(gtin14) ? gtin14 : null;
    }
}
