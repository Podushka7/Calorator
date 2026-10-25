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
    /// Добавление пустого ингредиента в рецепт (согласно диаграмме классов).
    /// </summary>
    public void AddIngredient()
    {
        AddIngredient(new RecipeIngredient { RecipeId = Id, AmountGrams = 100.0 });
    }

    /// <summary>
    /// Добавление ингредиента в рецепт с синхронизацией связей между сущностями.
    /// </summary>
    public void AddIngredient(RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        ingredient.RecipeId = Id;
        if (ingredient.Product != null && ingredient.ProductId == 0 && ingredient.Product.Id > 0)
        {
            ingredient.ProductId = ingredient.Product.Id;
        }

        Ingredients.Add(ingredient);
    }

    /// <summary>
    /// Добавление продукта в рецепт с указанием массы в граммах.
    /// </summary>
    public RecipeIngredient AddIngredient(Product product, double amountGrams = 100.0)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existing = product.Id > 0
            ? Ingredients.FirstOrDefault(i => i.ProductId == product.Id)
            : Ingredients.FirstOrDefault(i => ReferenceEquals(i.Product, product));

        if (existing != null)
        {
            existing.Product ??= product;
            existing.AmountGrams = Math.Round(existing.AmountGrams + amountGrams, 2);
            return existing;
        }

        var ingredient = new RecipeIngredient
        {
            RecipeId = Id,
            ProductId = product.Id,
            Product = product,
            AmountGrams = amountGrams
        };

        Ingredients.Add(ingredient);
        return ingredient;
    }

    /// <summary>
    /// Удаление последнего добавленного ингредиента из рецепта (согласно диаграмме классов).
    /// </summary>
    public void RemoveIngredient()
    {
        if (Ingredients.Count > 0)
        {
            Ingredients.RemoveAt(Ingredients.Count - 1);
        }
    }

    /// <summary>
    /// Удаление ингредиента из рецепта.
    /// </summary>
    public void RemoveIngredient(RecipeIngredient ingredient)
    {
        if (ingredient == null) return;
        if (!Ingredients.Remove(ingredient) && ingredient.Id > 0)
        {
            Ingredients.RemoveAll(i => i.Id == ingredient.Id);
        }
    }

    /// <summary>
    /// Удаление ингредиента из рецепта по продукту.
    /// </summary>
    public void RemoveIngredient(Product product)
    {
        if (product == null) return;
        Ingredients.RemoveAll(i =>
            ReferenceEquals(i.Product, product) ||
            (product.Id > 0 && i.ProductId == product.Id));
    }

    /// <summary>
    /// Удаление ингредиента из рецепта по идентификатору ингредиента или продукта.
    /// </summary>
    public bool RemoveIngredient(int ingredientOrProductId)
    {
        int removed = Ingredients.RemoveAll(i => i.Id == ingredientOrProductId);
        if (removed == 0)
        {
            removed = Ingredients.RemoveAll(i => i.ProductId == ingredientOrProductId);
        }
        return removed > 0;
    }

    /// <summary>
    /// Общая масса рецепта (г).
    /// </summary>
    public double TotalWeightGrams => Math.Round(Ingredients.Sum(i => i.AmountGrams), 2);

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
