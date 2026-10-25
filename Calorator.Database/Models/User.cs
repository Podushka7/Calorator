using Calorator.Database.Enums;

namespace Calorator.Database.Models;

/// <summary>
/// Учетная запись и профиль пользователя для расчета норм КБЖУ.
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Уникальный логин пользователя для авторизации.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Хэш пароля.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя пользователя.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Возраст в полных годах.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Пол (Male / Female).
    /// </summary>
    public Gender Gender { get; set; } = Gender.Male;

    /// <summary>
    /// Рост в сантиметрах.
    /// </summary>
    public double HeightCm { get; set; }

    /// <summary>
    /// Масса тела в килограммах.
    /// </summary>
    public double WeightKg { get; set; }

    /// <summary>
    /// Уровень физической активности.
    /// </summary>
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Moderate;

    /// <summary>
    /// Дата регистрации профиля.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Текущая рассчитанная суточная норма КБЖУ.
    /// </summary>
    public NutritionNorm? CurrentNutritionNorm { get; set; }

    /// <summary>
    /// Текущая активная цель по весу.
    /// </summary>
    public Goal? CurrentGoal { get; set; }

    /// <summary>
    /// Рецепты, созданные пользователем.
    /// </summary>
    public List<Recipe> Recipes { get; set; } = new();

    /// <summary>
    /// Записи дневника питания по дням.
    /// </summary>
    public List<DailyLog> DailyLogs { get; set; } = new();

    /// <summary>
    /// Актуализация профиля и пересчет суточной нормы КБЖУ (согласно диаграмме классов).
    /// </summary>
    public void UpdateProfile()
    {
        CalculateNutritionNorm();
    }

    /// <summary>
    /// Обновление параметров профиля с автоматическим пересчетом суточной нормы КБЖУ.
    /// </summary>
    public void UpdateProfile(string name, int age, Gender gender, double heightCm, double weightKg, ActivityLevel activityLevel)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }

        if (age > 0)
        {
            Age = age;
        }

        Gender = gender;

        if (heightCm > 0)
        {
            HeightCm = heightCm;
        }

        if (weightKg > 0)
        {
            WeightKg = weightKg;
        }

        ActivityLevel = activityLevel;

        CalculateNutritionNorm();
    }

    /// <summary>
    /// Применение текущей цели пользователя и пересчет нормы КБЖУ (согласно диаграмме классов).
    /// </summary>
    public void SetGoal()
    {
        CurrentGoal ??= new Goal
        {
            UserId = Id,
            User = this,
            Type = GoalType.Maintenance,
            TargetWeightKg = WeightKg,
            CalorieAdjustment = 0,
            CreatedAt = DateTime.Now
        };

        CurrentGoal.UserId = Id;
        CurrentGoal.User = this;
        CalculateNutritionNorm();
    }

    /// <summary>
    /// Установка новой цели с автоматическим пересчетом суточной нормы КБЖУ.
    /// </summary>
    public void SetGoal(Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        goal.UserId = Id;
        goal.User = this;
        if (goal.CalorieAdjustment == 0 && goal.Type != GoalType.Maintenance)
        {
            goal.CalorieAdjustment = Goal.GetDefaultCalorieAdjustment(goal.Type);
        }

        CurrentGoal = goal;
        CalculateNutritionNorm();
    }

    /// <summary>
    /// Установка новой цели по параметрам с автоматическим пересчетом суточной нормы КБЖУ.
    /// </summary>
    public Goal SetGoal(GoalType type, double targetWeightKg, int? calorieAdjustment = null)
    {
        var goal = new Goal
        {
            UserId = Id,
            User = this,
            Type = type,
            TargetWeightKg = targetWeightKg > 0 ? targetWeightKg : WeightKg,
            CalorieAdjustment = calorieAdjustment ?? Goal.GetDefaultCalorieAdjustment(type),
            CreatedAt = DateTime.Now
        };

        SetGoal(goal);
        return goal;
    }

    /// <summary>
    /// Рассчитывает и обновляет суточную норму КБЖУ пользователя по формуле Миффлина — Сан-Жеора с учетом цели.
    /// </summary>
    public NutritionNorm CalculateNutritionNorm()
    {
        CurrentNutritionNorm ??= new NutritionNorm
        {
            UserId = Id,
            User = this
        };

        CurrentNutritionNorm.UserId = Id;
        CurrentNutritionNorm.User = this;
        CurrentNutritionNorm.Recalculate(this, CurrentGoal);
        return CurrentNutritionNorm;
    }

    public override string ToString() => $"{Name} (@{Login}) - {WeightKg} кг, {HeightCm} см";
}
