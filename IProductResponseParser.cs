namespace NutritionApp.BarcodeRecognition;

/// <summary>Преобразует ответ конкретного сервиса 1С в модель продукта модуля.</summary>
public interface IProductResponseParser
{
    /// <summary>Разбирает ответ и проверяет его соответствие запрошенному GTIN-14; null при Found=false.</summary>
    ProductInfo? Parse(string response, string requestedBarcode);
}
