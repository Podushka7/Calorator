namespace NutritionApp.BarcodeRecognition;

/// <summary>Настройки HTTP-клиента для опубликованного сервиса 1С.</summary>
public sealed class OneCProductClientOptions
{
    /// <summary>
    /// URI или относительный URI шаблона запроса продукта. Должен содержать
    /// маркер <c>{barcode}</c>; авторизация и BaseAddress задаются через HttpClient.
    /// </summary>
    public string ProductByBarcodeUriTemplate { get; init; } = string.Empty;
}
