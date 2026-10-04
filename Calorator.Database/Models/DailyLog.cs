namespace Calorator.Database.Models;

/// <summary>
/// Дневник питания и гидратации пользователя за конкретный день.
/// </summary>
public class DailyLog
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Дата (день) записи дневника.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Приемы пищи за день.
    /// </summary>
    public List<Meal> Meals { get; set; } = new();

    /// <summary>
    /// Записи о выпитой воде за день.
    /// </summary>
    public List<WaterIntake> WaterIntakes { get; set; } = new();

    /// <summary>
    /// Суммарные калории за день (согласно диаграмме классов).
    /// </summary>
    public double GetTotalCalories() => Math.Round(Meals.Sum(m => m.GetCalories()), 2);

    /// <summary>
    /// Суммарные белки за день (согласно диаграмме классов).
    /// </summary>
    public double GetTotalProtein() => Math.Round(Meals.Sum(m => m.GetProtein()), 2);

    /// <summary>
    /// Суммарные жиры за день (согласно диаграмме классов).
    /// </summary>
    public double GetTotalFat() => Math.Round(Meals.Sum(m => m.GetFat()), 2);

    /// <summary>
    /// Суммарные углеводы за день (согласно диаграмме классов).
    /// </summary>
    public double GetTotalCarbohydrates() => Math.Round(Meals.Sum(m => m.GetCarbohydrates()), 2);

    /// <summary>
    /// Суммарный объем выпитой воды за день в миллилитрах (согласно диаграмме классов).
    /// </summary>
    public int GetTotalWater() => WaterIntakes.Sum(w => w.AmountMl);

    public override string ToString() => $"Дневник за {Date:yyyy-MM-dd}: {GetTotalCalories():F0} ккал, Вода: {GetTotalWater()} мл";
}
