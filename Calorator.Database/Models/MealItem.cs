namespace Calorator.Database.Models;

/// <summary>
/// Элемент приема пищи — конкретный продукт и его масса в граммах.
/// </summary>
public class MealItem
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор приема пищи.
    /// </summary>
    public int MealId { get; set; }

    /// <summary>
    /// Идентификатор продукта.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Связанный продукт (навигационное свойство).
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Масса порции в граммах.
    /// </summary>
    public double AmountGrams { get; set; }

    /// <summary>
    /// Калории для данной порции (ккал).
    /// </summary>
    public double Calories { get; set; }

    /// <summary>
    /// Белки для данной порции (г).
    /// </summary>
    public double Protein { get; set; }

    /// <summary>
    /// Жиры для данной порции (г).
    /// </summary>
    public double Fat { get; set; }

    /// <summary>
    /// Углеводы для данной порции (г).
    /// </summary>
    public double Carbohydrates { get; set; }

    /// <summary>
    /// Расчет пищевой ценности порции на основе продукта и граммовки (согласно диаграмме классов).
    /// </summary>
    public void CalculateNutrition(Product? product = null)
    {
        var targetProduct = product ?? Product;
        if (targetProduct == null || AmountGrams <= 0)
        {
            return;
        }

        double ratio = AmountGrams / 100.0;
        Calories = Math.Round(targetProduct.CaloriesPer100g * ratio, 2);
        Protein = Math.Round(targetProduct.ProteinPer100g * ratio, 2);
        Fat = Math.Round(targetProduct.FatPer100g * ratio, 2);
        Carbohydrates = Math.Round(targetProduct.CarbohydratesPer100g * ratio, 2);
    }

    public override string ToString() => $"{(Product?.Name ?? $"Продукт #{ProductId}")} - {AmountGrams}г ({Calories:F1} ккал)";
}
