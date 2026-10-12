using Calorator.Interface.Contracts;
using Calorator.Interface.Models;

namespace Calorator.Interface.Services;

/// <summary>
/// Implements the application contract using an in-memory product catalog and diary.
/// State is not persisted between application sessions.
/// </summary>
public sealed class InMemoryBusinessLogic : IBusinessLogic
{
    private readonly List<ProductInfo> _products =
    [
        new() { Name = "Яблоко", Barcode = "2000000000001", CaloriesPer100g = 52, ProteinPer100g = 0.3, FatPer100g = 0.2, CarbohydratesPer100g = 13.8 },
        new() { Name = "Банан", Barcode = "2000000000002", CaloriesPer100g = 89, ProteinPer100g = 1.1, FatPer100g = 0.3, CarbohydratesPer100g = 22.8 },
        new() { Name = "Овсяные хлопья", Barcode = "2000000000003", CaloriesPer100g = 370, ProteinPer100g = 13, FatPer100g = 7, CarbohydratesPer100g = 62 },
        new() { Name = "Гречка", Barcode = "2000000000004", CaloriesPer100g = 343, ProteinPer100g = 13.3, FatPer100g = 3.4, CarbohydratesPer100g = 71.5 },
        new() { Name = "Рис", Barcode = "2000000000005", CaloriesPer100g = 360, ProteinPer100g = 7, FatPer100g = 1, CarbohydratesPer100g = 79 },
        new() { Name = "Натуральный йогурт", Barcode = "2000000000006", CaloriesPer100g = 62, ProteinPer100g = 3.5, FatPer100g = 3.3, CarbohydratesPer100g = 4.7 },
        new() { Name = "Творог", Barcode = "2000000000007", CaloriesPer100g = 121, ProteinPer100g = 17, FatPer100g = 5, CarbohydratesPer100g = 2 },
        new() { Name = "Помидор", Barcode = "2000000000008", CaloriesPer100g = 18, ProteinPer100g = 0.9, FatPer100g = 0.2, CarbohydratesPer100g = 3.9 },
        new() { Name = "Огурец", Barcode = "2000000000009", CaloriesPer100g = 15, ProteinPer100g = 0.7, FatPer100g = 0.1, CarbohydratesPer100g = 3.6 },
        new() { Name = "Хлеб цельнозерновой", Barcode = "2000000000010", CaloriesPer100g = 247, ProteinPer100g = 13, FatPer100g = 4.2, CarbohydratesPer100g = 41 }
    ];

    private readonly List<DiaryEntry> _entries = [];
    private AppSettings _settings = new();
    private int _nextId = 1;

    public InMemoryBusinessLogic()
    {
        _entries.Add(new DiaryEntry { Id = _nextId++, Date = DateTime.Today, MealType = MealType.Breakfast, Product = _products[2], Grams = 75 });
        _entries.Add(new DiaryEntry { Id = _nextId++, Date = DateTime.Today, MealType = MealType.Snack, Product = _products[0], Grams = 140 });
    }

    public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<ProductInfo> result = _products.Where(p =>
            string.IsNullOrWhiteSpace(query) ||
            p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            p.Barcode.Contains(query, StringComparison.Ordinal)).ToList();
        return Task.FromResult(result);
    }

    public Task<ProductInfo?> FindByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_products.FirstOrDefault(p => p.Barcode == barcode));
    }

    public Task<ProductInfo?> RecognizeByPhotoAsync(string filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        throw new NotSupportedException("Распознавание фотографии недоступно: сервис не подключён.");
    }

    public Task<IReadOnlyList<DiaryEntry>> GetDiaryEntriesAsync(DateTime date, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<DiaryEntry> result = _entries.Where(e => e.Date.Date == date.Date).OrderBy(e => e.MealType).ThenBy(e => e.Id).ToList();
        return Task.FromResult(result);
    }

    public Task<DiaryEntry> AddToDiaryAsync(ProductInfo product, double grams, MealType mealType, DateTime date, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(product);
        var entry = new DiaryEntry { Id = _nextId++, Date = date.Date, MealType = mealType, Product = product, Grams = grams };
        _entries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task<bool> RemoveDiaryEntryAsync(int entryId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var removed = _entries.RemoveAll(e => e.Id == entryId) > 0;
        return Task.FromResult(removed);
    }

    public Task<NutritionSummary> GetStatisticsAsync(DateTime date, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var values = _entries.Where(e => e.Date.Date == date.Date).ToList();
        return Task.FromResult(new NutritionSummary
        {
            TotalCalories = values.Sum(e => e.Calories),
            TotalProtein = values.Sum(e => e.Protein),
            TotalFat = values.Sum(e => e.Fat),
            TotalCarbohydrates = values.Sum(e => e.Carbohydrates),
            EntriesCount = values.Count
        });
    }

    public Task<AppSettings> LoadSettingsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_settings.Clone());
    }

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _settings = settings.Clone();
        return Task.CompletedTask;
    }
}
