using Calorator.Interface.Models;
using Calorator.Interface.Services;
using Xunit;

namespace Calorator.Tests;

public sealed class StatisticsTests
{
    private static readonly DateTime TestDate = new(2042, 8, 22);

    private static ProductInfo Product() => new()
    {
        Name = "Тестовый продукт",
        CaloriesPer100g = 120,
        ProteinPer100g = 10,
        FatPer100g = 2,
        CarbohydratesPer100g = 15
    };

    [Fact]
    public async Task GetStatistics_EmptyDate_ReturnsZeroSummary()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var stats = await controller.GetStatistics(TestDate);

        Assert.Equal(0, stats.EntriesCount);
        Assert.Equal(0, stats.TotalCalories);
        Assert.Equal(0, stats.TotalProtein);
        Assert.Equal(0, stats.TotalFat);
        Assert.Equal(0, stats.TotalCarbohydrates);
    }

    [Fact]
    public async Task GetStatistics_InitialDiary_ReturnsSumOfExampleEntries()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var stats = await controller.GetStatistics(DateTime.Today);

        Assert.Equal(2, stats.EntriesCount);
        Assert.Equal(350.3, stats.TotalCalories, 6);
        Assert.Equal(10.17, stats.TotalProtein, 6);
        Assert.Equal(5.53, stats.TotalFat, 6);
        Assert.Equal(65.82, stats.TotalCarbohydrates, 6);
    }

    [Fact]
    public async Task GetStatistics_MultiplePortions_SumsAllNutrients()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        await controller.AddToDiary(Product(), 50, MealType.Breakfast, TestDate);
        await controller.AddToDiary(Product(), 150, MealType.Lunch, TestDate);

        var stats = await controller.GetStatistics(TestDate);

        Assert.Equal(2, stats.EntriesCount);
        Assert.Equal(240, stats.TotalCalories, 6);
        Assert.Equal(20, stats.TotalProtein, 6);
        Assert.Equal(4, stats.TotalFat, 6);
        Assert.Equal(30, stats.TotalCarbohydrates, 6);
    }

    [Fact]
    public async Task GetStatistics_AfterRemoval_RecalculatesTotals()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        var first = await controller.AddToDiary(Product(), 100, MealType.Breakfast, TestDate);
        await controller.AddToDiary(Product(), 200, MealType.Dinner, TestDate);
        await controller.RemoveEntry(first.Id);

        var stats = await controller.GetStatistics(TestDate);

        Assert.Equal(1, stats.EntriesCount);
        Assert.Equal(240, stats.TotalCalories, 6);
        Assert.Equal(20, stats.TotalProtein, 6);
    }

    [Fact]
    public async Task GetStatistics_FiltersEntriesByDate()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        await controller.AddToDiary(Product(), 100, MealType.Lunch, TestDate);
        await controller.AddToDiary(Product(), 200, MealType.Lunch, TestDate.AddDays(1));

        var stats = await controller.GetStatistics(TestDate);

        Assert.Equal(1, stats.EntriesCount);
        Assert.Equal(120, stats.TotalCalories, 6);
    }
}
