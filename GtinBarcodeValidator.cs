namespace NutritionApp.BarcodeRecognition;

/// <summary>Проверяет цифровые GTIN-8, GTIN-12, GTIN-13 и GTIN-14.</summary>
public static class GtinBarcodeValidator
{
    /// <summary>Приводит допустимый GTIN к 14 цифрам для единого ключа каталога.</summary>
    public static string ToGtin14(string barcode)
    {
        if (!IsValid(barcode))
        {
            throw new ArgumentException("Некорректный GTIN или контрольная цифра.", nameof(barcode));
        }

        return barcode.Trim().PadLeft(14, '0');
    }

    /// <summary>
    /// Возвращает true, если код содержит только цифры, имеет поддерживаемую длину
    /// и корректную контрольную цифру GS1. Начальные и конечные пробелы игнорируются.
    /// </summary>
    public static bool IsValid(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return false;
        }

        string value = barcode.Trim();
        if (value.Length is not (8 or 12 or 13 or 14))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        int weightedSum = 0;
        int weight = 3;
        for (int index = value.Length - 2; index >= 0; index--)
        {
            weightedSum += (value[index] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        int expectedCheckDigit = (10 - weightedSum % 10) % 10;
        return value[^1] - '0' == expectedCheckDigit;
    }
}
