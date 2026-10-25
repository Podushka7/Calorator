namespace NutritionApp.BarcodeRecognition;

/// <summary>Граница сетевого взаимодействия модуля с опубликованным сервисом 1С.</summary>
public interface IOneCProductClient
{
    /// <summary>Получает ответ 1С по GTIN-14; null означает HTTP 404.</summary>
    Task<string?> GetProductDataAsync(string barcode, CancellationToken cancellationToken = default);
}
