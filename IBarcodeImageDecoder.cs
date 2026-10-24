using System.Drawing;

namespace NutritionApp.BarcodeRecognition;

/// <summary>Граница декодирования кадра камеры, не отвечает за захват камеры.</summary>
public interface IBarcodeImageDecoder
{
    /// <summary>Возвращает распознанный код или null, если подходящего кода нет.</summary>
    DecodedBarcode? Decode(Bitmap frame);
}
