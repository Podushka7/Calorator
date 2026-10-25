using Calorator.Database.Enums;

namespace Calorator.Database.Models;

/// <summary>
/// Прием пищи (Завтрак, Обед, Ужин, Перекус) в рамках дневника дня.
/// </summary>
public class Meal
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор записи дневника (день).
    /// </summary>
    public int DailyLogId { get; set; }

    /// <summary>
    /// Тип приема пищи (Завтрак, Обед, Ужин, Перекус).
    /// </summary>
    public MealType Type { get; set; } = MealType.Breakfast;

    /// <summary>
    /// Время приема пищи.
    /// </summary>
    public TimeSpan Time { get; set; } = DateTime.Now.TimeOfDay;

    /// <summary>
    /// Продукты в данном приеме пищи.
    /// </summary>
    public List<MealItem> Items { get; set; } = new();

    /// <summary>
    /// Добавление пустого элемента в прием пищи (согласно диаграмме классов).
    /// </summary>
    public void AddItem()
    {
        AddItem(new MealItem { MealId = Id });
    }

    /// <summary>
    /// Добавление продукта в прием пищи с автоматическим расчетом КБЖУ порции.
    /// </summary>
    public void AddItem(MealItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.MealId = Id;

        if (item.Product != null)
        {
            if (item.ProductId == 0 && item.Product.Id > 0)
            {
                item.ProductId = item.Product.Id;
            }

            if (item.AmountGrams > 0 && item.Calories == 0 && item.Protein == 0 && item.Fat == 0 && item.Carbohydrates == 0)
            {
                item.CalculateNutrition();
            }
        }

        Items.Add(item);
    }

    /// <summary>
    /// Добавление продукта с указанной граммовкой в прием пищи.
    /// </summary>
    public MealItem AddItem(Product product, double amountGrams)
    {
        ArgumentNullException.ThrowIfNull(product);

        var item = new MealItem
        {
            MealId = Id,
            ProductId = product.Id,
            Product = product,
            AmountGrams = amountGrams
        };
        item.CalculateNutrition(product);
        Items.Add(item);
        return item;
    }

    /// <summary>
    /// Удаление последнего добавленного продукта из приема пищи (согласно диаграмме классов).
    /// </summary>
    public void RemoveItem()
    {
        if (Items.Count > 0)
        {
            Items.RemoveAt(Items.Count - 1);
        }
    }

    /// <summary>
    /// Удаление продукта из приема пищи.
    /// </summary>
    public void RemoveItem(MealItem item)
    {
        if (item == null) return;

        if (!Items.Remove(item) && item.Id > 0)
        {
            Items.RemoveAll(i => i.Id == item.Id);
        }
    }

    /// <summary>
    /// Удаление продукта из приема пищи по идентификатору элемента.
    /// </summary>
    public bool RemoveItem(int mealItemId)
    {
        return Items.RemoveAll(i => i.Id == mealItemId) > 0;
    }

    /// <summary>
    /// Суммарные калории приема пищи (согласно диаграмме классов).
    /// </summary>
    public double GetCalories() => Math.Round(Items.Sum(i => i.Calories), 2);

    /// <summary>
    /// Суммарные белки приема пищи (согласно диаграмме классов).
    /// </summary>
    public double GetProtein() => Math.Round(Items.Sum(i => i.Protein), 2);

    /// <summary>
    /// Суммарные жиры приема пищи (согласно диаграмме классов).
    /// </summary>
    public double GetFat() => Math.Round(Items.Sum(i => i.Fat), 2);

    /// <summary>
    /// Суммарные углеводы приема пищи (согласно диаграмме классов).
    /// </summary>
    public double GetCarbohydrates() => Math.Round(Items.Sum(i => i.Carbohydrates), 2);

    public override string ToString() => $"{Type} ({Time:hh\\:mm}) - {Items.Count} прод., {GetCalories():F0} ккал";
}
