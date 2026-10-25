namespace NutritionApp.BarcodeRecognition
{
    /// <summary>Нормализованные сведения о продукте, полученные по его штрихкоду.</summary>
    public sealed class ProductInfo
    {
        public string Name { get; set; } = string.Empty;

        public string Barcode { get; set; } = string.Empty;

        /// <summary>Неизвестное значение — null, а не ноль.</summary>
        public double? CaloriesPer100g { get; set; }

        public double? ProteinPer100g { get; set; }

        public double? FatPer100g { get; set; }

        public double? CarbohydratesPer100g { get; set; }
    }
}
