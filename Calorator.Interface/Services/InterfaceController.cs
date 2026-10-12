using Calorator.Interface.Contracts;
using Calorator.Interface.Models;

namespace Calorator.Interface.Services;

/// <summary>Methods mirror the class diagram; UI never queries SQLite directly.</summary>
public sealed class InterfaceController
{
    private readonly IBusinessLogic _businessLogic;

    public InterfaceController(IBusinessLogic businessLogic)
    {
        _businessLogic = businessLogic ?? throw new ArgumentNullException(nameof(businessLogic));
    }

    public Task<IReadOnlyList<ProductInfo>> SearchProducts(string query, CancellationToken ct = default) =>
        _businessLogic.SearchProductsAsync(query.Trim(), ct);

    public Task<ProductInfo?> FindByBarcode(string code, CancellationToken ct = default) =>
        _businessLogic.FindByBarcodeAsync(code.Trim(), ct);

    public Task<ProductInfo?> RecognizeByPhoto(string filePath, CancellationToken ct = default) =>
        _businessLogic.RecognizeByPhotoAsync(filePath, ct);

    public Task<IReadOnlyList<DiaryEntry>> GetDiaryEntries(DateTime date, CancellationToken ct = default) =>
        _businessLogic.GetDiaryEntriesAsync(date.Date, ct);

    public Task<DiaryEntry> AddToDiary(ProductInfo product, double grams, MealType mealType, DateTime date, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (grams <= 0 || double.IsNaN(grams) || double.IsInfinity(grams))
            throw new ArgumentOutOfRangeException(nameof(grams), "Масса порции должна быть положительной.");
        return _businessLogic.AddToDiaryAsync(product, grams, mealType, date.Date, ct);
    }

    public Task<bool> RemoveEntry(int entryId, CancellationToken ct = default) =>
        _businessLogic.RemoveDiaryEntryAsync(entryId, ct);

    public Task<NutritionSummary> GetStatistics(DateTime date, CancellationToken ct = default) =>
        _businessLogic.GetStatisticsAsync(date.Date, ct);

    public Task<AppSettings> LoadSettings(CancellationToken ct = default) =>
        _businessLogic.LoadSettingsAsync(ct);

    public Task SaveSettings(AppSettings settings, CancellationToken ct = default) =>
        _businessLogic.SaveSettingsAsync(settings, ct);
}
