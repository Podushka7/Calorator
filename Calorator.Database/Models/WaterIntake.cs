namespace Calorator.Database.Models;

/// <summary>
/// Запись о выпитой воде (в миллилитрах) с фиксацией времени.
/// </summary>
public class WaterIntake
{
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор записи дневника (день).
    /// </summary>
    public int DailyLogId { get; set; }

    /// <summary>
    /// Количество выпитой воды в миллилитрах.
    /// </summary>
    public int AmountMl { get; set; }

    /// <summary>
    /// Время приема воды.
    /// </summary>
    public TimeSpan Time { get; set; } = DateTime.Now.TimeOfDay;

    public override string ToString() => $"{AmountMl} мл в {Time:hh\\:mm}";
}
