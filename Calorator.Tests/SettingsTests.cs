using Calorator.Interface.Models;
using Calorator.Interface.Services;
using Xunit;

namespace Calorator.Tests;

public sealed class SettingsTests
{
    [Fact]
    public async Task LoadSettings_NewInstance_HasDefaultProfile()
    {
        var service = new InMemoryBusinessLogic();

        var settings = await service.LoadSettingsAsync();

        Assert.Equal("Пользователь", settings.DisplayName);
        Assert.Null(settings.DailyCalorieGoal);
    }

    [Fact]
    public async Task SaveSettings_PersistsValuesWithinOneInstance()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());
        await controller.SaveSettings(new AppSettings { DisplayName = "Тест", DailyCalorieGoal = 2100 });

        var settings = await controller.LoadSettings();

        Assert.Equal("Тест", settings.DisplayName);
        Assert.True(settings.DailyCalorieGoal.HasValue);
        Assert.Equal(2100d, settings.DailyCalorieGoal.Value);
    }

    [Fact]
    public async Task SaveSettings_MakesDefensiveCopyOfInput()
    {
        var service = new InMemoryBusinessLogic();
        var input = new AppSettings { DisplayName = "Исходное имя", DailyCalorieGoal = 1800 };
        await service.SaveSettingsAsync(input);
        input.DisplayName = "Изменено снаружи";
        input.DailyCalorieGoal = 500;

        var saved = await service.LoadSettingsAsync();

        Assert.Equal("Исходное имя", saved.DisplayName);
        Assert.True(saved.DailyCalorieGoal.HasValue);
        Assert.Equal(1800d, saved.DailyCalorieGoal.Value);
    }

    [Fact]
    public async Task LoadSettings_ReturnsIndependentCopy()
    {
        var service = new InMemoryBusinessLogic();
        await service.SaveSettingsAsync(new AppSettings { DisplayName = "Настройка" });
        var first = await service.LoadSettingsAsync();
        first.DisplayName = "Локальное изменение";

        var second = await service.LoadSettingsAsync();

        Assert.Equal("Настройка", second.DisplayName);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task NewServiceInstance_HasIndependentSettings()
    {
        var firstService = new InMemoryBusinessLogic();
        var secondService = new InMemoryBusinessLogic();
        await firstService.SaveSettingsAsync(new AppSettings { DisplayName = "Первый" });

        var secondSettings = await secondService.LoadSettingsAsync();

        Assert.Equal("Пользователь", secondSettings.DisplayName);
    }

    [Fact]
    public async Task SaveSettings_NullSettings_IsRejected()
    {
        var controller = new InterfaceController(new InMemoryBusinessLogic());

        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.SaveSettings(null!));
    }

    [Fact]
    public async Task LoadSettings_WhenCancelled_ThrowsOperationCanceledException()
    {
        var service = new InMemoryBusinessLogic();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.LoadSettingsAsync(cancellation.Token));
    }
}
