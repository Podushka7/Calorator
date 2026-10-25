using Calorator.Database.Enums;
using Calorator.Database.Models;
using Xunit;

namespace Calorator.Database.Tests.Unit;

/// <summary>
/// Модульные (Unit) тесты для всех сущностей, расчетов КБЖУ, целей, рецептов, приемов пищи и дневника.
/// </summary>
public class DomainModelsUnitTests
{
    #region User & NutritionNorm & Goal (Mifflin-St Jeor)

    [Theory]
    [InlineData(Gender.Male, 80.0, 180.0, 30, 1780.0)]   // 10*80 + 6.25*180 - 5*30 + 5 = 800 + 1125 - 150 + 5 = 1780
    [InlineData(Gender.Female, 60.0, 165.0, 25, 1345.25)] // 10*60 + 6.25*165 - 5*25 - 161 = 600 + 1031.25 - 125 - 161 = 1345.25
    public void CalculateBmr_ValidParameters_ReturnsExpectedMifflinStJeorValue(
        Gender gender, double weight, double height, int age, double expectedBmr)
    {
        double actual = NutritionNorm.CalculateBmr(gender, weight, height, age);
        Assert.Equal(expectedBmr, actual);
    }

    [Theory]
    [InlineData(Gender.Male, 0, 180, 25)]
    [InlineData(Gender.Male, 80, 0, 25)]
    [InlineData(Gender.Female, 60, 165, 0)]
    [InlineData(Gender.Female, -10, -165, -5)]
    public void CalculateBmr_InvalidZeroOrNegativeParameters_ReturnsZero(
        Gender gender, double weight, double height, int age)
    {
        double actual = NutritionNorm.CalculateBmr(gender, weight, height, age);
        Assert.Equal(0.0, actual);
    }

    [Theory]
    [InlineData(ActivityLevel.Sedentary, 1.2)]
    [InlineData(ActivityLevel.Light, 1.375)]
    [InlineData(ActivityLevel.Moderate, 1.55)]
    [InlineData(ActivityLevel.High, 1.725)]
    [InlineData(ActivityLevel.VeryHigh, 1.9)]
    [InlineData((ActivityLevel)999, 1.2)]
    public void GetActivityMultiplier_ReturnsExpectedCoefficient(ActivityLevel level, double expected)
    {
        Assert.Equal(expected, NutritionNorm.GetActivityMultiplier(level));
    }

    [Fact]
    public void CalculateTdee_ValidUser_CalculatesCorrectMaintenanceCalories()
    {
        var user = new User
        {
            Gender = Gender.Male,
            WeightKg = 80.0,
            HeightCm = 180.0,
            Age = 30,
            ActivityLevel = ActivityLevel.Moderate // 1.55
        };

        // BMR = 1780, TDEE = 1780 * 1.55 = 2759
        double tdee = NutritionNorm.CalculateTdee(user);
        Assert.Equal(2759.0, tdee);
    }

    [Fact]
    public void CalculateTdee_NullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NutritionNorm.CalculateTdee(null!));
    }

    [Fact]
    public void User_UpdateProfile_UpdatesFieldsAndRecalculatesNorm()
    {
        var user = new User
        {
            Id = 5,
            Login = "test_user",
            Name = "Old Name",
            Age = 20,
            Gender = Gender.Female,
            HeightCm = 160,
            WeightKg = 55,
            ActivityLevel = ActivityLevel.Sedentary
        };

        user.UpdateProfile("  Иван Иванов  ", 30, Gender.Male, 180.0, 80.0, ActivityLevel.Moderate);

        Assert.Equal("Иван Иванов", user.Name);
        Assert.Equal(30, user.Age);
        Assert.Equal(Gender.Male, user.Gender);
        Assert.Equal(180.0, user.HeightCm);
        Assert.Equal(80.0, user.WeightKg);
        Assert.Equal(ActivityLevel.Moderate, user.ActivityLevel);
        Assert.NotNull(user.CurrentNutritionNorm);
        Assert.Equal(2759.0, user.CurrentNutritionNorm.Calories);
        Assert.Equal(5, user.CurrentNutritionNorm.UserId);
        Assert.Same(user, user.CurrentNutritionNorm.User);
        Assert.Contains("Иван Иванов (@test_user)", user.ToString());
    }

    [Fact]
    public void User_UpdateProfile_Parameterless_RecalculatesNorm()
    {
        var user = new User
        {
            Id = 1,
            Login = "ivan",
            Name = "Иван",
            Age = 30,
            Gender = Gender.Male,
            HeightCm = 180.0,
            WeightKg = 80.0,
            ActivityLevel = ActivityLevel.Sedentary // 1.2 -> 1780 * 1.2 = 2136
        };

        user.UpdateProfile();

        Assert.NotNull(user.CurrentNutritionNorm);
        Assert.Equal(2136.0, user.CurrentNutritionNorm.Calories);
    }

    [Fact]
    public void User_UpdateProfile_WithInvalidInputs_KeepsExistingValidValues()
    {
        var user = new User
        {
            Name = "Original",
            Age = 25,
            HeightCm = 175.0,
            WeightKg = 70.0
        };

        user.UpdateProfile("   ", -5, Gender.Male, 0, -10, ActivityLevel.High);

        Assert.Equal("Original", user.Name);
        Assert.Equal(25, user.Age);
        Assert.Equal(175.0, user.HeightCm);
        Assert.Equal(70.0, user.WeightKg);
    }

    [Fact]
    public void User_SetGoal_AllOverloads_AdjustTargetCaloriesProperly()
    {
        var user = new User
        {
            Id = 10,
            Login = "athlete",
            Name = "Атлет",
            Age = 30,
            Gender = Gender.Male,
            HeightCm = 180.0,
            WeightKg = 80.0,
            ActivityLevel = ActivityLevel.Moderate // TDEE = 2759
        };

        // 1. Parameterless SetGoal() creates default Maintenance goal
        user.SetGoal();
        Assert.NotNull(user.CurrentGoal);
        Assert.Equal(GoalType.Maintenance, user.CurrentGoal.Type);
        Assert.Equal(2759.0, user.CurrentNutritionNorm!.Calories);

        // 2. SetGoal(GoalType.WeightLoss, targetWeightKg)
        var lossGoal = user.SetGoal(GoalType.WeightLoss, 75.0);
        Assert.Equal(GoalType.WeightLoss, lossGoal.Type);
        Assert.Equal(-500, lossGoal.CalorieAdjustment);
        Assert.Equal(2259.0, user.CurrentNutritionNorm.Calories);

        // 3. SetGoal(Goal) with WeightGain
        var gainGoal = new Goal { Type = GoalType.WeightGain, TargetWeightKg = 85.0 };
        user.SetGoal(gainGoal);
        Assert.Equal(300, gainGoal.CalorieAdjustment);
        Assert.Equal(3059.0, user.CurrentNutritionNorm.Calories);
        Assert.Contains("WeightGain", gainGoal.ToString());
    }

    [Fact]
    public void Goal_CalculateTargetCalories_WorksWithAndWithoutUser()
    {
        var standaloneGoal = new Goal
        {
            Type = GoalType.WeightLoss,
            TargetWeightKg = 65.0
        };

        // Without user: uses 2000 base - 500 default adjustment = 1500
        Assert.Equal(1500.0, standaloneGoal.CalculateTargetCalories());

        // Extreme negative adjustment floors at 500 kcal
        standaloneGoal.CalorieAdjustment = -3000;
        Assert.Equal(500.0, standaloneGoal.CalculateTargetCalories(1800.0));

        var user = new User
        {
            Id = 3,
            Gender = Gender.Male,
            WeightKg = 80.0,
            HeightCm = 180.0,
            Age = 30,
            ActivityLevel = ActivityLevel.Sedentary // 2136
        };

        var userGoal = new Goal
        {
            Type = GoalType.WeightGain,
            CalorieAdjustment = 400
        };

        Assert.Equal(2536.0, userGoal.CalculateTargetCalories(user));
        Assert.Equal(2536.0, userGoal.CalculateTargetCalories());
    }

    [Fact]
    public void NutritionNorm_Recalculate_AllOverloads_ComputeCorrectMacros()
    {
        var norm = new NutritionNorm { Calories = 2000.0 };
        norm.Recalculate();

        // 2000 kcal -> Protein: 30% / 4 = 150g, Fat: 25% / 9 = 55.56g, Carbs: 45% / 4 = 225g
        Assert.Equal(150.0, norm.Protein);
        Assert.Equal(55.56, norm.Fat);
        Assert.Equal(225.0, norm.Carbohydrates);

        norm.Recalculate(1800.126, 120.555, 60.444, 190.111);
        Assert.Equal(1800.13, norm.Calories);
        Assert.Equal(120.56, norm.Protein);
        Assert.Equal(60.44, norm.Fat);
        Assert.Equal(190.11, norm.Carbohydrates);
        Assert.Contains("Норма:", norm.ToString());
    }

    #endregion

    #region Product, MealItem, Meal, DailyLog, WaterIntake

    [Fact]
    public void Product_PropertiesAndToString_FormatCorrectly()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Творог 5%",
            CaloriesPer100g = 121.0,
            ProteinPer100g = 17.0,
            FatPer100g = 5.0,
            CarbohydratesPer100g = 3.0,
            Barcode = "4607001000042"
        };

        string str = product.ToString();
        Assert.Contains("Творог 5%", str);
        Assert.Contains("121", str);
    }

    [Fact]
    public void MealItem_CalculateNutrition_HandlesValidAndZeroAmounts()
    {
        var product = new Product
        {
            Id = 7,
            Name = "Гречка",
            CaloriesPer100g = 313.0,
            ProteinPer100g = 12.6,
            FatPer100g = 3.3,
            CarbohydratesPer100g = 62.1
        };

        var item = new MealItem
        {
            AmountGrams = 150.0
        };

        item.CalculateNutrition(product);

        Assert.Equal(7, item.ProductId);
        Assert.Same(product, item.Product);
        Assert.Equal(469.5, item.Calories);
        Assert.Equal(18.9, item.Protein);
        Assert.Equal(4.95, item.Fat);
        Assert.Equal(93.15, item.Carbohydrates);
        Assert.Contains("Гречка", item.ToString());

        // When AmountGrams becomes 0, macros reset to 0
        item.AmountGrams = 0;
        item.CalculateNutrition();
        Assert.Equal(0, item.Calories);
        Assert.Equal(0, item.Protein);
        Assert.Equal(0, item.Fat);
        Assert.Equal(0, item.Carbohydrates);
    }

    [Fact]
    public void Meal_AddAndRemoveItems_CalculatesTotalsCorrectly()
    {
        var product1 = new Product
        {
            Id = 1,
            Name = "Яйцо",
            CaloriesPer100g = 150.0,
            ProteinPer100g = 12.0,
            FatPer100g = 10.0,
            CarbohydratesPer100g = 1.0
        };
        var product2 = new Product
        {
            Id = 2,
            Name = "Хлеб",
            CaloriesPer100g = 250.0,
            ProteinPer100g = 8.0,
            FatPer100g = 2.0,
            CarbohydratesPer100g = 50.0
        };

        var meal = new Meal
        {
            Id = 11,
            DailyLogId = 3,
            Type = MealType.Breakfast,
            Time = new TimeSpan(8, 15, 0)
        };

        // Add via Product + grams
        var item1 = meal.AddItem(product1, 100.0);
        item1.Id = 101;

        // Add via MealItem instance
        var item2 = new MealItem { Id = 102, Product = product2, AmountGrams = 50.0 };
        meal.AddItem(item2);

        Assert.Equal(2, meal.Items.Count);
        Assert.Equal(275.0, meal.GetCalories());        // 150 + 125
        Assert.Equal(16.0, meal.GetProtein());          // 12 + 4
        Assert.Equal(11.0, meal.GetFat());              // 10 + 1
        Assert.Equal(26.0, meal.GetCarbohydrates());    // 1 + 25
        Assert.Contains("Breakfast", meal.ToString());

        // Parameterless AddItem & RemoveItem
        meal.AddItem();
        Assert.Equal(3, meal.Items.Count);
        meal.RemoveItem();
        Assert.Equal(2, meal.Items.Count);

        // Remove by ID
        Assert.True(meal.RemoveItem(101));
        Assert.Single(meal.Items);

        // Remove by MealItem reference / ID
        meal.RemoveItem(new MealItem { Id = 102 });
        Assert.Empty(meal.Items);

        // Edge cases for Meal & MealItem
        meal.RemoveItem(); // empty list safe
        meal.RemoveItem((MealItem)null!);
        Assert.False(meal.RemoveItem(9999));

        var orphanItem = new MealItem { ProductId = 77, AmountGrams = 50 };
        orphanItem.CalculateNutrition();
        Assert.Contains("Продукт #77", orphanItem.ToString());
    }

    [Fact]
    public void DailyLog_MealsAndWaterIntakes_AggregatesDailyTotals()
    {
        var log = new DailyLog
        {
            Id = 50,
            UserId = 1,
            Date = new DateTime(2026, 10, 25)
        };

        var product = new Product
        {
            Id = 1,
            Name = "Курица",
            CaloriesPer100g = 200,
            ProteinPer100g = 30,
            FatPer100g = 8,
            CarbohydratesPer100g = 2
        };

        var breakfast = new Meal { Id = 1, Type = MealType.Breakfast };
        breakfast.AddItem(product, 100); // 200 kcal

        var dinner = new Meal { Id = 2, Type = MealType.Dinner };
        dinner.AddItem(product, 150); // 300 kcal

        log.AddMeal(breakfast);
        log.AddMeal(dinner);

        var w1 = log.AddWaterIntake(250, new TimeSpan(9, 0, 0));
        w1.Id = 10;
        var w2 = new WaterIntake { Id = 11, AmountMl = 350, Time = new TimeSpan(14, 0, 0) };
        log.AddWaterIntake(w2);

        Assert.Equal(500.0, log.GetTotalCalories());
        Assert.Equal(75.0, log.GetTotalProtein());
        Assert.Equal(20.0, log.GetTotalFat());
        Assert.Equal(5.0, log.GetTotalCarbohydrates());
        Assert.Equal(600, log.GetTotalWater());
        Assert.Contains("2026-10-25", log.ToString());
        Assert.Contains("250 мл", w1.ToString());

        log.RemoveWaterIntake(new WaterIntake { Id = 10 });
        Assert.Equal(350, log.GetTotalWater());

        log.RemoveMeal(new Meal { Id = 1 });
        Assert.Equal(300.0, log.GetTotalCalories());
    }

    #endregion

    #region Recipe, RecipeIngredient & NutritionSummary

    [Fact]
    public void Recipe_IngredientManagementAndCalculateNutrition_ComputesPerServingNorm()
    {
        var oats = new Product
        {
            Id = 1,
            Name = "Овсянка",
            CaloriesPer100g = 350,
            ProteinPer100g = 12,
            FatPer100g = 6,
            CarbohydratesPer100g = 60
        };
        var milk = new Product
        {
            Id = 2,
            Name = "Молоко",
            CaloriesPer100g = 50,
            ProteinPer100g = 3,
            FatPer100g = 2.5,
            CarbohydratesPer100g = 5
        };

        var recipe = new Recipe
        {
            Id = 7,
            UserId = 2,
            Name = "Овсяная каша на молоке",
            Servings = 2
        };

        recipe.AddIngredient(oats, 100); // 350 kcal
        // Adding same product again merges weight
        recipe.AddIngredient(oats, 100); // total 200g oats = 700 kcal
        var milkIng = recipe.AddIngredient(milk, 200); // 100 kcal
        milkIng.Id = 55;

        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Equal(400.0, recipe.TotalWeightGrams);

        // Total: 800 kcal, 30g protein, 17g fat, 130g carbs. Per 1 of 2 servings:
        var perServing = recipe.CalculateNutrition();
        Assert.Equal(400.0, perServing.Calories);
        Assert.Equal(15.0, perServing.Protein);
        Assert.Equal(8.5, perServing.Fat);
        Assert.Equal(65.0, perServing.Carbohydrates);
        Assert.Contains("Овсяная каша", recipe.ToString());
        Assert.Contains("Молоко", milkIng.ToString());

        // Parameterless AddIngredient & RemoveIngredient
        recipe.AddIngredient();
        Assert.Equal(3, recipe.Ingredients.Count);
        recipe.RemoveIngredient();
        Assert.Equal(2, recipe.Ingredients.Count);

        // Remove by Product
        recipe.RemoveIngredient(oats);
        Assert.Single(recipe.Ingredients);

        // Remove by ID
        Assert.True( recipe.RemoveIngredient(55));
        Assert.Empty(recipe.Ingredients);

        // Edge cases for Recipe & RecipeIngredient without Product
        var orphanIng = new RecipeIngredient { ProductId = 99, AmountGrams = 50 };
        Assert.Equal(0, orphanIng.Calories);
        Assert.Equal(0, orphanIng.Protein);
        Assert.Equal(0, orphanIng.Fat);
        Assert.Equal(0, orphanIng.Carbohydrates);
        Assert.Contains("Продукт #99", orphanIng.ToString());

        recipe.AddIngredient(orphanIng);
        recipe.RemoveIngredient(new RecipeIngredient { Id = 999 });
        recipe.RemoveIngredient((RecipeIngredient)null!);
        recipe.RemoveIngredient((Product)null!);
        Assert.True(recipe.RemoveIngredient(99)); // removes by ProductId fallback
        Assert.False(recipe.RemoveIngredient(9999));
    }

    [Fact]
    public void NutritionSummary_AveragesAndFormatting_WorkWithZeroAndPositiveDays()
    {
        var summary = new NutritionSummary
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 4),
            TotalCalories = 8000,
            TotalProtein = 400,
            TotalFat = 240,
            TotalCarbohydrates = 900,
            TotalWaterMl = 6000,
            DaysCount = 4
        };

        Assert.Equal(2000.0, summary.AverageDailyCalories);
        Assert.Equal(1500.0, summary.AverageDailyWaterMl);
        Assert.Contains("8000", summary.ToString());

        var zeroDaysSummary = new NutritionSummary
        {
            TotalCalories = 500,
            TotalWaterMl = 300,
            DaysCount = 0
        };
        Assert.Equal(500.0, zeroDaysSummary.AverageDailyCalories);
        Assert.Equal(300.0, zeroDaysSummary.AverageDailyWaterMl);
    }

    #endregion
}
