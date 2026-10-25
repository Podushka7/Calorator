using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace NutritionApp.BarcodeRecognition;

/// <summary>Читает EAN-13 и Data Matrix из готового кадра камеры.</summary>
public sealed class ZxingBarcodeImageDecoder : IBarcodeImageDecoder
{
    public DecodedBarcode? Decode(Bitmap frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var reader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat>
                {
                    BarcodeFormat.EAN_13,
                    BarcodeFormat.DATA_MATRIX
                }
            }
        };

        Result? result = reader.Decode(frame);
        if (result is null)
        {
            return null;
        }

        if (result.BarcodeFormat == BarcodeFormat.EAN_13 &&
            result.Text.Length == 13 &&
            GtinBarcodeValidator.IsValid(result.Text))
        {
            return new DecodedBarcode(GtinBarcodeValidator.ToGtin14(result.Text), BarcodeKind.Ean13);
        }

        if (result.BarcodeFormat == BarcodeFormat.DATA_MATRIX)
        {
            string? gtin14 = Gs1DataMatrixParser.TryGetGtin14(result.Text);
            if (gtin14 is not null)
            {
                return new DecodedBarcode(gtin14, BarcodeKind.Gs1DataMatrix);
            }
        }

        return null;
    }
}
