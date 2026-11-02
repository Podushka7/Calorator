using Calorator.Interface.Models;
using Xunit;

namespace Calorator.Tests;

public sealed class ModelTests
{
    [Fact]
    public void DiaryEntry_NutritionProperties_ScaleWithPortionSize()
    {
        var entry = new DiaryEntry
        {
            Product = new ProductInfo
            {
                Name = "Продукт",
                CaloriesPer100g = 120,
                ProteinPer100g = 10,
                FatPer100g = 4,
                CarbohydratesPer100g = 15
            },
            Grams = 250
        };

        Assert.Equal(300, entry.Calories, 6);
        Assert.Equal(25, entry.Protein, 6);
        Assert.Equal(10, entry.Fat, 6);
        Assert.Equal(37.5, entry.Carbohydrates, 6);
    }

    [Fact]
    public void ProductInfo_ToString_ReturnsProductName()
    {
        var product = new ProductInfo { Name = "Гречка" };

        Assert.Equal("Гречка", product.ToString());
    }

    [Theory]
    [InlineData(MealType.Breakfast, "Завтрак")]
    [InlineData(MealType.Lunch, "Обед")]
    [InlineData(MealType.Dinner, "Ужин")]
    [InlineData(MealType.Snack, "Перекус")]
    [InlineData((MealType)99, "Приём пищи")]
    public void MealType_ToRussian_ReturnsExpectedLabel(MealType mealType, string expectedLabel)
    {
        Assert.Equal(expectedLabel, mealType.ToRussian());
    }

    [Fact]
    public void AppSettings_Clone_CopiesFieldsIntoNewObject()
    {
        var original = new AppSettings { DisplayName = "Тест", DailyCalorieGoal = 2400 };

        var copy = original.Clone();

        Assert.NotSame(original, copy);
        Assert.Equal(original.DisplayName, copy.DisplayName);
        Assert.Equal(original.DailyCalorieGoal, copy.DailyCalorieGoal);
    }
}
