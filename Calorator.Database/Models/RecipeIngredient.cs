namespace Calorator.Database.Models;

/// <summary>
/// Ингредиент рецепта с указанием продукта и массы в граммах.
/// </summary>
public class RecipeIngredient
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор рецепта.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Идентификатор продукта.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Связанный продукт (навигационное свойство).
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Масса ингредиента в граммах.
    /// </summary>
    public double AmountGrams { get; set; }

    /// <summary>
    /// Расчет калорийности ингредиента.
    /// </summary>
    public double Calories => Product != null ? Math.Round(Product.CaloriesPer100g * (AmountGrams / 100.0), 2) : 0;

    /// <summary>
    /// Расчет белков ингредиента.
    /// </summary>
    public double Protein => Product != null ? Math.Round(Product.ProteinPer100g * (AmountGrams / 100.0), 2) : 0;

    /// <summary>
    /// Расчет жиров ингредиента.
    /// </summary>
    public double Fat => Product != null ? Math.Round(Product.FatPer100g * (AmountGrams / 100.0), 2) : 0;

    /// <summary>
    /// Расчет углеводов ингредиента.
    /// </summary>
    public double Carbohydrates => Product != null ? Math.Round(Product.CarbohydratesPer100g * (AmountGrams / 100.0), 2) : 0;

    public override string ToString() => $"{(Product?.Name ?? $"Продукт #{ProductId}")} - {AmountGrams}г";
}
