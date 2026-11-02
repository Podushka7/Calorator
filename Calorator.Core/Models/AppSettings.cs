namespace Calorator.Interface.Models;

public sealed class AppSettings
{
    public string DisplayName { get; set; } = "Пользователь";
    public double? DailyCalorieGoal { get; set; }

    public AppSettings Clone() => new()
    {
        DisplayName = DisplayName,
        DailyCalorieGoal = DailyCalorieGoal
    };
}
