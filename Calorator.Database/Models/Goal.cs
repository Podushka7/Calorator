using Calorator.Database.Enums;

namespace Calorator.Database.Models;

/// <summary>
/// Цель пользователя по массе тела и корректировке калорийности.
/// </summary>
public class Goal
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Тип цели: Снижение веса, Поддержание веса, Набор веса.
    /// </summary>
    public GoalType Type { get; set; } = GoalType.Maintenance;

    /// <summary>
    /// Желаемая целевая масса тела (кг).
    /// </summary>
    public double TargetWeightKg { get; set; }

    /// <summary>
    /// Корректировка калорийности (ккал), например -500 ккал для дефицита или +300 ккал для набора.
    /// </summary>
    public int CalorieAdjustment { get; set; }

    /// <summary>
    /// Дата установки цели.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Расчет целевой калорийности с учетом корректировки (согласно диаграмме классов).
    /// </summary>
    public double CalculateTargetCalories(double maintenanceCalories)
    {
        return Math.Max(500, maintenanceCalories + CalorieAdjustment);
    }

    public override string ToString() => $"{Type}: цель {TargetWeightKg:F1} кг (корректировка {CalorieAdjustment:+0;-0;0} ккал)";
}
