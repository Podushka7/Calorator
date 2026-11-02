using Calorator.Interface.Models;
using Calorator.Interface.Services;
using Xunit;

namespace Calorator.Tests;

public sealed class InterfaceControllerTests
{
    [Fact]
    public void Constructor_WithoutBusinessLogic_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InterfaceController(null!));
    }

    [Fact]
    public async Task SearchProducts_RemovesLeadingAndTrailingWhitespace()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var matches = await controller.SearchProducts("   яблоко   ");

        Assert.Single(matches);
        Assert.Equal("Яблоко", matches[0].Name);
    }

    [Fact]
    public async Task FindByBarcode_RemovesLeadingAndTrailingWhitespace()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        var product = await controller.FindByBarcode("  2000000000002  ");

        Assert.NotNull(product);
        Assert.Equal("Банан", product.Name);
    }

    [Fact]
    public async Task SearchProducts_NullQuery_ThrowsArgumentNullException()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.SearchProducts(null!));
    }

    [Fact]
    public async Task FindByBarcode_NullCode_ThrowsArgumentNullException()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.FindByBarcode(null!));
    }

    [Fact]
    public async Task GetDiaryEntries_NormalizesDateTimeToCalendarDay()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        var date = new DateTime(2042, 7, 20, 8, 45, 0);
        await controller.AddToDiary(new ProductInfo { Name = "Продукт" }, 100, MealType.Lunch, date);

        var entries = await controller.GetDiaryEntries(date.AddHours(12));

        Assert.Single(entries);
        Assert.Equal(date.Date, entries[0].Date);
    }
}
