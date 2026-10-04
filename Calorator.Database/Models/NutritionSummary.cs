namespace Calorator.Database.Models;

/// <summary>
/// Агрегированные итоги КБЖУ и потребления воды за выбранный период или прием пищи.
/// </summary>
public class NutritionSummary
{
    /// <summary>
    /// Начальная дата периода (если применимо).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Конечная дата периода (если применимо).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Суммарные калории (ккал).
    /// </summary>
    public double TotalCalories { get; set; }

    /// <summary>
    /// Суммарные белки (г).
    /// </summary>
    public double TotalProtein { get; set; }

    /// <summary>
    /// Суммарные жиры (г).
    /// </summary>
    public double TotalFat { get; set; }

    /// <summary>
    /// Суммарные углеводы (г).
    /// </summary>
    public double TotalCarbohydrates { get; set; }

    /// <summary>
    /// Суммарное количество выпитой воды (мл).
    /// </summary>
    public int TotalWaterMl { get; set; }

    /// <summary>
    /// Количество дней в периоде с записями.
    /// </summary>
    public int DaysCount { get; set; }

    /// <summary>
    /// Среднесуточная калорийность (ккал/день).
    /// </summary>
    public double AverageDailyCalories => DaysCount > 0 ? Math.Round(TotalCalories / DaysCount, 1) : TotalCalories;

    /// <summary>
    /// Среднесуточный объем воды (мл/день).
    /// </summary>
    public double AverageDailyWaterMl => DaysCount > 0 ? Math.Round((double)TotalWaterMl / DaysCount, 0) : TotalWaterMl;

    public override string ToString() =>
        $"КБЖУ: {TotalCalories:F0} ккал | Б: {TotalProtein:F1}г | Ж: {TotalFat:F1}г | У: {TotalCarbohydrates:F1}г | Вода: {TotalWaterMl} мл";
}
