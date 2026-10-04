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
    /// Метод пересчета нормы (согласно диаграмме классов).
    /// </summary>
    public void Recalculate(double calories, double protein, double fat, double carbohydrates)
    {
        Calories = calories;
        Protein = protein;
        Fat = fat;
        Carbohydrates = carbohydrates;
        CalculatedAt = DateTime.Now;
    }

    public override string ToString() => $"Норма: {Calories:F0} ккал (Б: {Protein:F1}г, Ж: {Fat:F1}г, У: {Carbohydrates:F1}г)";
}
