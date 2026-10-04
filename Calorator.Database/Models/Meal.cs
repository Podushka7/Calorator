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
    /// Добавление продукта в прием пищи (согласно диаграмме классов).
    /// </summary>
    public void AddItem(MealItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.MealId = Id;
        Items.Add(item);
    }

    /// <summary>
    /// Удаление продукта из приема пищи (согласно диаграмме классов).
    /// </summary>
    public void RemoveItem(MealItem item)
    {
        Items.Remove(item);
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
