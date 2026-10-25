using System.Drawing;

namespace NutritionApp.BarcodeRecognition;

/// <summary>Публичный контракт модуля поиска продукта по штрихкоду.</summary>
public interface IProductRecognitionService
{
    /// <summary>Проверяет поддерживаемый формат GTIN и контрольную цифру.</summary>
    bool ValidateBarcode(string barcode);

    /// <summary>Получает продукт по GTIN из 1С; null означает, что продукт не найден.</summary>
    Task<ProductInfo?> RecognizeProductAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>Декодирует кадр камеры и ищет продукт, если код обнаружен.</summary>
    Task<BarcodeScanResult> RecognizeFromImageAsync(Bitmap frame, CancellationToken cancellationToken = default);
}
