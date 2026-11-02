using Calorator.Interface.Services;
using Xunit;

namespace Calorator.Tests;

public sealed class ProductCatalogTests
{
    [Fact]
    public async Task SearchProducts_EmptyQuery_ReturnsEntireCatalog()
    {
        var service = new InMemoryBusinessLogic();

        var products = await service.SearchProductsAsync(string.Empty);

        Assert.Equal(10, products.Count);
        Assert.Contains(products, product => product.Name == "Яблоко");
    }

    [Theory]
    [InlineData("ЯБЛОКО", "Яблоко")]
    [InlineData("бан", "Банан")]
    [InlineData("оВся", "Овсяные хлопья")]
    public async Task SearchProducts_ByName_IsCaseInsensitive(string query, string expectedName)
    {
        var service = new InMemoryBusinessLogic();

        var products = await service.SearchProductsAsync(query);

        Assert.Contains(products, product => product.Name == expectedName);
    }

    [Fact]
    public async Task SearchProducts_ByBarcodeFragment_FindsMatchingProduct()
    {
        var service = new InMemoryBusinessLogic();

        var products = await service.SearchProductsAsync("0000000009");

        Assert.Single(products);
        Assert.Equal("Огурец", products[0].Name);
    }

    [Fact]
    public async Task SearchProducts_UnknownQuery_ReturnsEmptyCollection()
    {
        var service = new InMemoryBusinessLogic();

        var products = await service.SearchProductsAsync("несуществующий продукт");

        Assert.Empty(products);
    }

    [Fact]
    public async Task FindByBarcode_KnownCode_ReturnsProduct()
    {
        var service = new InMemoryBusinessLogic();

        var product = await service.FindByBarcodeAsync("2000000000002");

        Assert.NotNull(product);
        Assert.Equal("Банан", product.Name);
        Assert.Equal(89, product.CaloriesPer100g);
    }

    [Fact]
    public async Task FindByBarcode_UnknownCode_ReturnsNull()
    {
        var service = new InMemoryBusinessLogic();

        var product = await service.FindByBarcodeAsync("9999999999999");

        Assert.Null(product);
    }

    [Fact]
    public async Task RecognizeByPhoto_WhenServiceIsNotConnected_ThrowsNotSupportedException()
    {
        var service = new InMemoryBusinessLogic();

        var error = await Assert.ThrowsAsync<NotSupportedException>(
            async () => await service.RecognizeByPhotoAsync("photo.jpg"));

        Assert.Contains("не подключён", error.Message);
    }

    [Fact]
    public async Task SearchProducts_WhenCancelled_ThrowsOperationCanceledException()
    {
        var service = new InMemoryBusinessLogic();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.SearchProductsAsync("яблоко", cancellation.Token));
    }

    [Fact]
    public async Task FindByBarcode_WhenCancelled_ThrowsOperationCanceledException()
    {
        var service = new InMemoryBusinessLogic();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.FindByBarcodeAsync("2000000000001", cancellation.Token));
    }
}
