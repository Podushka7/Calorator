namespace Calorator.Interface.Models;

/// <summary>A displayable diary record, independent of the persistence layer.</summary>
public sealed class DiaryEntry
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public MealType MealType { get; init; }
    public ProductInfo Product { get; init; } = new();
    public double Grams { get; init; }

    public double Calories => Product.CaloriesPer100g * Grams / 100;
    public double Protein => Product.ProteinPer100g * Grams / 100;
    public double Fat => Product.FatPer100g * Grams / 100;
    public double Carbohydrates => Product.CarbohydratesPer100g * Grams / 100;
}
