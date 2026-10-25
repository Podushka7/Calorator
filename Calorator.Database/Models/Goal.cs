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
    /// Связанный пользователь (для передачи данных при расчете целевой калорийности).
    /// </summary>
    public User? User { get; set; }

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
    /// Возвращает стандартную корректировку калорийности для выбранного типа цели.
    /// </summary>
    public static int GetDefaultCalorieAdjustment(GoalType type) => type switch
    {
        GoalType.WeightLoss => -500,
        GoalType.WeightGain => 300,
        _ => 0
    };

    /// <summary>
    /// Расчет целевой калорийности на основе связанного профиля пользователя (согласно диаграмме классов).
    /// </summary>
    public double CalculateTargetCalories()
    {
        if (User != null)
        {
            return CalculateTargetCalories(User);
        }

        return CalculateTargetCalories(2000.0);
    }

    /// <summary>
    /// Расчет целевой калорийности для указанного пользователя по формуле Миффлина — Сан-Жеора.
    /// </summary>
    public double CalculateTargetCalories(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        User = user;
        UserId = user.Id;

        double maintenanceCalories = NutritionNorm.CalculateTdee(user);
        return CalculateTargetCalories(maintenanceCalories);
    }

    /// <summary>
    /// Расчет целевой калорийности с учетом корректировки от суточной нормы поддержания.
    /// </summary>
    public double CalculateTargetCalories(double maintenanceCalories)
    {
        int adjustment = CalorieAdjustment != 0
            ? CalorieAdjustment
            : GetDefaultCalorieAdjustment(Type);

        return Math.Max(500.0, Math.Round(maintenanceCalories + adjustment, 2));
    }

    public override string ToString() => $"{Type}: цель {TargetWeightKg:F1} кг (корректировка {CalorieAdjustment:+0;-0;0} ккал)";
}
