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
    /// Обновление параметров профиля (из диаграммы классов).
    /// </summary>
    public void UpdateProfile(string name, int age, Gender gender, double heightCm, double weightKg, ActivityLevel activityLevel)
    {
        Name = name;
        Age = age;
        Gender = gender;
        HeightCm = heightCm;
        WeightKg = weightKg;
        ActivityLevel = activityLevel;
    }

    /// <summary>
    /// Установка новой цели (из диаграммы классов).
    /// </summary>
    public void SetGoal(Goal goal)
    {
        CurrentGoal = goal;
    }

    public override string ToString() => $"{Name} (@{Login}) - {WeightKg} кг, {HeightCm} см";
}
