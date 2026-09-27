namespace NutritionApp.AI
{
    // Информация о продукте,
    // полученная после распознавания штрихкода.
    public class ProductInfo
    {
        public string Name { get; set; }

        public string Barcode { get; set; }

        public double CaloriesPer100g { get; set; }

        public double ProteinPer100g { get; set; }

        public double FatPer100g { get; set; }

        public double CarbohydratesPer100g { get; set; }
    }
}