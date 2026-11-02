using Calorator.Interface.Models;

namespace Calorator.Interface.Contracts;

/// <summary>
/// Defines operations used by the interface independently from data storage and recognition services.
/// </summary>
public interface IBusinessLogic
{
    Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string query, CancellationToken ct = default);
    Task<ProductInfo?> FindByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<ProductInfo?> RecognizeByPhotoAsync(string filePath, CancellationToken ct = default);
    Task<IReadOnlyList<DiaryEntry>> GetDiaryEntriesAsync(DateTime date, CancellationToken ct = default);
    Task<DiaryEntry> AddToDiaryAsync(ProductInfo product, double grams, MealType mealType, DateTime date, CancellationToken ct = default);
    Task<bool> RemoveDiaryEntryAsync(int entryId, CancellationToken ct = default);
    Task<NutritionSummary> GetStatisticsAsync(DateTime date, CancellationToken ct = default);
    Task<AppSettings> LoadSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default);
}
