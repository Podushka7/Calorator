namespace Calorator.Database.Models;

/// <summary>
/// Продукт питания с содержанием КБЖУ на 100 грамм.
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>
    /// Название продукта.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Калорийность на 100г (ккал).
    /// </summary>
    public double CaloriesPer100g { get; set; }

    /// <summary>
    /// Белки на 100г (г).
    /// </summary>
    public double ProteinPer100g { get; set; }

    /// <summary>
    /// Жиры на 100г (г).
    /// </summary>
    public double FatPer100g { get; set; }

    /// <summary>
    /// Углеводы на 100г (г).
    /// </summary>
    public double CarbohydratesPer100g { get; set; }

    /// <summary>
    /// Штрихкод продукта (для распознавания через сканер/API).
    /// </summary>
    public string? Barcode { get; set; }

    public override string ToString() => $"{Name} ({CaloriesPer100g:F0} ккал, Б: {ProteinPer100g:F1}, Ж: {FatPer100g:F1}, У: {CarbohydratesPer100g:F1})";
}
