using Calorator.Interface.Models;
using Calorator.Interface.Services;
using Xunit;

namespace Calorator.Tests;

public sealed class DiaryTests
{
    private static readonly DateTime TestDate = new(2042, 5, 19);

    private static ProductInfo NewProduct() => new()
    {
        Name = "Тестовый продукт",
        Barcode = "999000111",
        CaloriesPer100g = 200,
        ProteinPer100g = 10,
        FatPer100g = 4,
        CarbohydratesPer100g = 20
    };

    [Fact]
    public async Task GetDiaryEntries_DefaultState_HasTwoExampleEntriesForToday()
    {
        var service = new InMemoryBusinessLogic();

        var entries = await service.GetDiaryEntriesAsync(DateTime.Today);

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry.Product.Name == "Овсяные хлопья");
        Assert.Contains(entries, entry => entry.Product.Name == "Яблоко");
    }

    [Fact]
    public async Task AddToDiary_PersistsEntryWithCorrectDateAndPortion()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        var product = NewProduct();

        var added = await controller.AddToDiary(product, 150, MealType.Lunch, TestDate.AddHours(14));
        var entries = await controller.GetDiaryEntries(TestDate);

        Assert.Single(entries);
        Assert.Equal(added.Id, entries[0].Id);
        Assert.Equal(TestDate, added.Date);
        Assert.Equal(MealType.Lunch, added.MealType);
        Assert.Same(product, added.Product);
        Assert.Equal(150, added.Grams);
    }

    [Fact]
    public async Task AddToDiary_ConsecutiveEntries_ReceiveDistinctIds()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var first = await controller.AddToDiary(NewProduct(), 100, MealType.Lunch, TestDate);
        var second = await controller.AddToDiary(NewProduct(), 200, MealType.Dinner, TestDate);

        Assert.True(first.Id > 0);
        Assert.True(second.Id > first.Id);
    }

    [Fact]
    public async Task GetDiaryEntries_OrdersByMealThenCreationOrder()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var dinner = await controller.AddToDiary(NewProduct(), 100, MealType.Dinner, TestDate);
        var firstBreakfast = await controller.AddToDiary(NewProduct(), 50, MealType.Breakfast, TestDate);
        var secondBreakfast = await controller.AddToDiary(NewProduct(), 75, MealType.Breakfast, TestDate);
        var entries = await controller.GetDiaryEntries(TestDate);

        Assert.Equal(new[] { firstBreakfast.Id, secondBreakfast.Id, dinner.Id }, entries.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetDiaryEntries_DoesNotMixDifferentDates()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        await controller.AddToDiary(NewProduct(), 100, MealType.Lunch, TestDate);
        await controller.AddToDiary(NewProduct(), 150, MealType.Lunch, TestDate.AddDays(1));

        var dayOne = await controller.GetDiaryEntries(TestDate);
        var dayTwo = await controller.GetDiaryEntries(TestDate.AddDays(1));

        Assert.Single(dayOne);
        Assert.Single(dayTwo);
        Assert.Equal(100, dayOne[0].Grams);
        Assert.Equal(150, dayTwo[0].Grams);
    }

    [Fact]
    public async Task RemoveEntry_ExistingEntry_RemovesItFromDiary()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        var entry = await controller.AddToDiary(NewProduct(), 125, MealType.Snack, TestDate);

        var deleted = await controller.RemoveEntry(entry.Id);
        var entries = await controller.GetDiaryEntries(TestDate);

        Assert.True(deleted);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task RemoveEntry_UnknownId_ReturnsFalse()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var deleted = await controller.RemoveEntry(-999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task RemoveEntry_DeletesOnlySelectedEntry()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        var first = await controller.AddToDiary(NewProduct(), 100, MealType.Lunch, TestDate);
        var second = await controller.AddToDiary(NewProduct(), 200, MealType.Dinner, TestDate);

        await controller.RemoveEntry(first.Id);
        var remaining = await controller.GetDiaryEntries(TestDate);

        Assert.Single(remaining);
        Assert.Equal(second.Id, remaining[0].Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-200)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task AddToDiary_InvalidWeight_IsRejectedByController(double grams)
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            controller.AddToDiary(NewProduct(), grams, MealType.Lunch, TestDate));
    }

    [Fact]
    public async Task AddToDiary_NullProduct_IsRejectedByController()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            controller.AddToDiary(null!, 100, MealType.Lunch, TestDate));
    }

    [Fact]
    public async Task AddToDiary_InvalidWeight_IsAlsoRejectedByBusinessLogic()
    {
        var service = new InMemoryBusinessLogic();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.AddToDiaryAsync(NewProduct(), -10, MealType.Lunch, TestDate));
    }
}
