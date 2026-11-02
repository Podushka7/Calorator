namespace Calorator.Interface.Models;

public sealed class NutritionSummary
{
    public double TotalCalories { get; init; }
    public double TotalProtein { get; init; }
    public double TotalFat { get; init; }
    public double TotalCarbohydrates { get; init; }
    public int EntriesCount { get; init; }
}
