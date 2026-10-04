namespace Calorator.Database.Models;

/// <summary>
/// Рецепт блюда, состоящий из списка продуктов и их количества.
/// </summary>
public class Recipe
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя-создателя рецепта.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Название рецепта.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Количество порций в рецепте.
    /// </summary>
    public int Servings { get; set; } = 1;

    /// <summary>
    /// Список ингредиентов рецепта.
    /// </summary>
    public List<RecipeIngredient> Ingredients { get; set; } = new();

    /// <summary>
    /// Добавление ингредиента в рецепт (согласно диаграмме классов).
    /// </summary>
    public void AddIngredient(RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        ingredient.RecipeId = Id;
        Ingredients.Add(ingredient);
    }

    /// <summary>
    /// Удаление ингредиента из рецепта (согласно диаграмме классов).
    /// </summary>
    public void RemoveIngredient(RecipeIngredient ingredient)
    {
        Ingredients.Remove(ingredient);
    }

    /// <summary>
    /// Общая масса рецепта (г).
    /// </summary>
    public double TotalWeightGrams => Ingredients.Sum(i => i.AmountGrams);

    /// <summary>
    /// Расчет пищевой ценности на 1 порцию рецепта (согласно диаграмме классов).
    /// </summary>
    public NutritionNorm CalculateNutrition()
    {
        int safeServings = Math.Max(1, Servings);
        double totalCalories = Ingredients.Sum(i => i.Calories);
        double totalProtein = Ingredients.Sum(i => i.Protein);
        double totalFat = Ingredients.Sum(i => i.Fat);
        double totalCarbs = Ingredients.Sum(i => i.Carbohydrates);

        return new NutritionNorm
        {
            UserId = UserId,
            Calories = Math.Round(totalCalories / safeServings, 2),
            Protein = Math.Round(totalProtein / safeServings, 2),
            Fat = Math.Round(totalFat / safeServings, 2),
            Carbohydrates = Math.Round(totalCarbs / safeServings, 2),
            CalculatedAt = DateTime.Now
        };
    }

    public override string ToString() => $"{Name} ({Servings} порц., {Ingredients.Count} ингред.)";
}
