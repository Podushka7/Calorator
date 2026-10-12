namespace Calorator.Interface.Models;

public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Snack
}

public static class MealTypeText
{
    public static string ToRussian(this MealType type) => type switch
    {
        MealType.Breakfast => "Завтрак",
        MealType.Lunch => "Обед",
        MealType.Dinner => "Ужин",
        MealType.Snack => "Перекус",
        _ => "Приём пищи"
    };
}
