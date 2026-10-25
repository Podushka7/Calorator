using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using Xunit;

namespace NutritionApp.BarcodeRecognition.Tests;

public sealed class ZxingBarcodeImageDecoderTests
{
    [Fact]
    public void Decode_reads_ean13_and_returns_gtin14()
    {
        const string ean13 = "4640009499755";
        BitMatrix matrix = new MultiFormatWriter().encode(ean13, BarcodeFormat.EAN_13, 520, 160);
        using Bitmap bitmap = ToBitmap(matrix);

        DecodedBarcode decoded = Assert.IsType<DecodedBarcode>(new ZxingBarcodeImageDecoder().Decode(bitmap));

        Assert.Equal("04640009499755", decoded.Gtin14);
        Assert.Equal(BarcodeKind.Ean13, decoded.Kind);
    }

    [Fact]
    public void Decode_reads_data_matrix_ai_01_and_returns_gtin14()
    {
        const string gs1Payload = "\u001d0104640009499755";
        BitMatrix matrix = new MultiFormatWriter().encode(gs1Payload, BarcodeFormat.DATA_MATRIX, 240, 240);
        using Bitmap bitmap = ToBitmap(matrix);

        DecodedBarcode decoded = Assert.IsType<DecodedBarcode>(new ZxingBarcodeImageDecoder().Decode(bitmap));

        Assert.Equal("04640009499755", decoded.Gtin14);
        Assert.Equal(BarcodeKind.Gs1DataMatrix, decoded.Kind);
    }

    [Fact]
    public void Decode_returns_null_for_blank_image()
    {
        using var bitmap = new Bitmap(40, 40);

        Assert.Null(new ZxingBarcodeImageDecoder().Decode(bitmap));
    }

    [Fact]
    public void Decode_returns_null_for_data_matrix_without_ai_01_gtin()
    {
        BitMatrix matrix = new MultiFormatWriter().encode(
            "\u001d9904640009499755",
            BarcodeFormat.DATA_MATRIX,
            240,
            240);
        using Bitmap bitmap = ToBitmap(matrix);

        Assert.Null(new ZxingBarcodeImageDecoder().Decode(bitmap));
    }

    [Fact]
    public void Decode_throws_for_null_frame()
    {
        Assert.Throws<ArgumentNullException>(() => new ZxingBarcodeImageDecoder().Decode(null!));
    }

    private static Bitmap ToBitmap(BitMatrix matrix)
    {
        var bitmap = new Bitmap(matrix.Width, matrix.Height, PixelFormat.Format24bppRgb);
        for (int y = 0; y < matrix.Height; y++)
        {
            for (int x = 0; x < matrix.Width; x++)
            {
                bitmap.SetPixel(x, y, matrix[x, y] ? Color.Black : Color.White);
            }
        }

        return bitmap;
    }
}
