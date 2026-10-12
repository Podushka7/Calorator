namespace Calorator.Interface.Models;

/// <summary>
/// UI model. Maps 1:1 to NutritionApp.AI.ProductInfo in the Bogdan/main branch.
/// It is intentionally separate to prevent duplicate class names during integration.
/// </summary>
public sealed class ProductInfo
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public double CaloriesPer100g { get; set; }
    public double ProteinPer100g { get; set; }
    public double FatPer100g { get; set; }
    public double CarbohydratesPer100g { get; set; }

    public override string ToString() => Name;
}
