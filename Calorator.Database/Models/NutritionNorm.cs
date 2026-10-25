using Calorator.Database.Enums;

namespace Calorator.Database.Models;

/// <summary>
/// Суточная норма КБЖУ пользователя, рассчитанная по формуле Миффлина — Сан-Жеора с учетом цели.
/// </summary>
public class NutritionNorm
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Связанный пользователь (для автоматического пересчета норм по данным профиля).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Суточная норма калорий (ккал).
    /// </summary>
    public double Calories { get; set; }

    /// <summary>
    /// Суточная норма белков (г).
    /// </summary>
    public double Protein { get; set; }

    /// <summary>
    /// Суточная норма жиров (г).
    /// </summary>
    public double Fat { get; set; }

    /// <summary>
    /// Суточная норма углеводов (г).
    /// </summary>
    public double Carbohydrates { get; set; }

    /// <summary>
    /// Дата и время расчета.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Рассчитывает базовый обмен веществ (BMR) по формуле Миффлина — Сан-Жеора.
    /// </summary>
    public static double CalculateBmr(Gender gender, double weightKg, double heightCm, int age)
    {
        if (weightKg <= 0 || heightCm <= 0 || age <= 0)
        {
            return 0.0;
        }

        double baseValue = (10.0 * weightKg) + (6.25 * heightCm) - (5.0 * age);
        double genderOffset = gender == Gender.Male ? 5.0 : -161.0;

        return Math.Max(500.0, Math.Round(baseValue + genderOffset, 2));
    }

    /// <summary>
    /// Возвращает коэффициент физической активности для формулы Миффлина — Сан-Жеора.
    /// </summary>
    public static double GetActivityMultiplier(ActivityLevel activityLevel) => activityLevel switch
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.Light => 1.375,
        ActivityLevel.Moderate => 1.55,
        ActivityLevel.High => 1.725,
        ActivityLevel.VeryHigh => 1.9,
        _ => 1.2
    };

    /// <summary>
    /// Рассчитывает суточный расход калорий на поддержание массы тела (TDEE) пользователя.
    /// </summary>
    public static double CalculateTdee(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        double bmr = CalculateBmr(user.Gender, user.WeightKg, user.HeightCm, user.Age);
        double multiplier = GetActivityMultiplier(user.ActivityLevel);
        return Math.Round(bmr * multiplier, 2);
    }

    /// <summary>
    /// Пересчет нормы КБЖУ на основе связанного пользователя и его текущей цели (согласно диаграмме классов).
    /// </summary>
    public void Recalculate()
    {
        if (User != null)
        {
            Recalculate(User, User.CurrentGoal);
            return;
        }

        if (Calories > 0)
        {
            Protein = Math.Round((Calories * 0.30) / 4.0, 2);
            Fat = Math.Round((Calories * 0.25) / 9.0, 2);
            Carbohydrates = Math.Round((Calories * 0.45) / 4.0, 2);
            CalculatedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Пересчет суточной нормы КБЖУ пользователя по формуле Миффлина — Сан-Жеора с учетом цели.
    /// </summary>
    public void Recalculate(User user, Goal? goal = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        User = user;
        UserId = user.Id;

        double tdee = CalculateTdee(user);
        Goal? activeGoal = goal ?? user.CurrentGoal;

        double targetCalories = activeGoal != null
            ? activeGoal.CalculateTargetCalories(tdee)
            : tdee;

        Calories = Math.Round(targetCalories, 2);
        // Распределение БЖУ: 30% белки (4 ккал/г), 25% жиры (9 ккал/г), 45% углеводы (4 ккал/г)
        Protein = Math.Round((Calories * 0.30) / 4.0, 2);
        Fat = Math.Round((Calories * 0.25) / 9.0, 2);
        Carbohydrates = Math.Round((Calories * 0.45) / 4.0, 2);
        CalculatedAt = DateTime.Now;
    }

    /// <summary>
    /// Пересчет нормы с явной передачей значений КБЖУ.
    /// </summary>
    public void Recalculate(double calories, double protein, double fat, double carbohydrates)
    {
        Calories = Math.Round(calories, 2);
        Protein = Math.Round(protein, 2);
        Fat = Math.Round(fat, 2);
        Carbohydrates = Math.Round(carbohydrates, 2);
        CalculatedAt = DateTime.Now;
    }

    public override string ToString() => $"Норма: {Calories:F0} ккал (Б: {Protein:F1}г, Ж: {Fat:F1}г, У: {Carbohydrates:F1}г)";
}
