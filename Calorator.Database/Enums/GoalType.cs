namespace Calorator.Database.Enums;

/// <summary>
/// Цель пользователя по массе тела.
/// </summary>
public enum GoalType
{
    /// <summary>
    /// Снижение массы тела (дефицит калорий)
    /// </summary>
    WeightLoss = 0,

    /// <summary>
    /// Поддержание текущей массы тела
    /// </summary>
    Maintenance = 1,

    /// <summary>
    /// Набор массы тела (профицит калорий)
    /// </summary>
    WeightGain = 2
}
