using Calorator.Database.Contracts;
using Calorator.Database.Enums;
using Calorator.Database.Implementation;
using Calorator.Database.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Calorator.Database.Tests.Integration;

/// <summary>
/// Полные интеграционные тесты для SqliteDataAccess (покрывают все функциональные и нефункциональные требования).
/// </summary>
public class SqliteDataAccessTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbFilePath;
    private readonly IDataBase _database;
    private readonly IDataAccess _dataAccess;

    public SqliteDataAccessTests()
    {
        _dbFilePath = Path.Combine(Path.GetTempPath(), $"calorator_test_{Guid.NewGuid():N}.db");
        _database = new SqliteDatabase($"Data Source={_dbFilePath};Foreign Keys=True;");
        _dataAccess = new SqliteDataAccess(_database);
    }

    public async Task InitializeAsync()
    {
        await _database.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbFilePath))
        {
            try
            {
                File.Delete(_dbFilePath);
            }
            catch
            {
                // ignore temp cleanup in test runner
            }
        }
    }

    [Fact]
    public void Constructors_WithConnectionStringOrNullDatabase_BehaveCorrectly()
    {
        var directAccess = new SqliteDataAccess($"Data Source={_dbFilePath};");
        Assert.NotNull(directAccess.Database);

        Assert.Throws<ArgumentNullException>(() => new SqliteDataAccess((IDataBase)null!));
    }

    [Fact]
    public async Task Requirement1_UserRegistration_ProfileUpdate_PasswordChange_AndCascadeDelete()
    {
        var user = new User
        {
            Login = "alex_fitness",
            PasswordHash = "hash_abc_123",
            Name = "Алексей Смирнов",
            Age = 28,
            Gender = Gender.Male,
            HeightCm = 182.5,
            WeightKg = 84.0,
            ActivityLevel = ActivityLevel.Moderate,
            CurrentGoal = new Goal
            {
                Type = GoalType.WeightLoss,
                TargetWeightKg = 78.0
            }
        };

        int userId = await _dataAccess.CreateUserAsync(user);
        Assert.True(userId > 0);
        Assert.NotNull(user.CurrentNutritionNorm);
        Assert.True(user.CurrentNutritionNorm.Id > 0);

        var retrieved = await _dataAccess.GetUserByIdAsync(userId);
        Assert.NotNull(retrieved);
        Assert.Equal("alex_fitness", retrieved.Login);
        Assert.Equal("Алексей Смирнов", retrieved.Name);
        Assert.Equal(28, retrieved.Age);
        Assert.Equal(Gender.Male, retrieved.Gender);
        Assert.Equal(182.5, retrieved.HeightCm);
        Assert.Equal(84.0, retrieved.WeightKg);
        Assert.Equal(ActivityLevel.Moderate, retrieved.ActivityLevel);
        Assert.NotNull(retrieved.CurrentGoal);
        Assert.Equal(GoalType.WeightLoss, retrieved.CurrentGoal.Type);
        Assert.NotNull(retrieved.CurrentNutritionNorm);

        // Поиск по логину (регистронезависимо)
        var byLogin = await _dataAccess.GetUserByLoginAsync("ALEX_FITNESS");
        Assert.NotNull(byLogin);
        Assert.Equal(userId, byLogin.Id);

        // UserExists
        Assert.True(await _dataAccess.UserExistsAsync("alex_fitness"));
        Assert.False(await _dataAccess.UserExistsAsync("nobody"));
        Assert.False(await _dataAccess.UserExistsAsync("   "));

        // Обновление профиля автоматически пересчитывает и сохраняет новую норму КБЖУ
        user.UpdateProfile("Алексей Петров", 29, Gender.Male, 183.0, 80.0, ActivityLevel.High);
        bool updated = await _dataAccess.UpdateUserProfileAsync(user);
        Assert.True(updated);

        var normsHistory = await _dataAccess.GetNutritionNormHistoryByUserIdAsync(userId);
        Assert.True(normsHistory.Count >= 2);

        // Обновление пароля
        bool passUpdated = await _dataAccess.UpdatePasswordAsync(userId, "new_secure_hash_999");
        Assert.True(passUpdated);

        var afterPassUpdate = await _dataAccess.GetUserByIdAsync(userId);
        Assert.Equal("new_secure_hash_999", afterPassUpdate!.PasswordHash);

        // Удаление пользователя
        bool deleted = await _dataAccess.DeleteUserAsync(userId);
        Assert.True(deleted);
        Assert.Null(await _dataAccess.GetUserByIdAsync(userId));
    }

    [Fact]
    public async Task Requirement2And8_NutritionNormsAndGoalsHistory_WorkAndAutoRecalculate()
    {
        var user = new User
        {
            Login = "maria_k",
            PasswordHash = "hash_pass",
            Name = "Мария",
            Age = 25,
            Gender = Gender.Female,
            HeightCm = 168.0,
            WeightKg = 60.0,
            ActivityLevel = ActivityLevel.Light
        };
        int userId = await _dataAccess.CreateUserAsync(user);

        // Сохранение кастомной нормы с привязанным User и Calories = 0 (автопересчет)
        var autoNorm = new NutritionNorm { UserId = userId, User = user };
        int normId = await _dataAccess.SaveNutritionNormAsync(autoNorm);
        Assert.True(normId > 0);
        Assert.True(autoNorm.Calories > 0);

        // Установка нескольких целей
        var goal1 = new Goal
        {
            UserId = userId,
            Type = GoalType.WeightLoss,
            TargetWeightKg = 55.0
        };
        int g1Id = await _dataAccess.SetGoalAsync(goal1);
        Assert.True(g1Id > 0);
        Assert.Equal(-500, goal1.CalorieAdjustment);

        var goal2 = new Goal
        {
            UserId = userId,
            Type = GoalType.WeightGain,
            TargetWeightKg = 63.0
        };
        int g2Id = await _dataAccess.SetGoalAsync(goal2);
        Assert.True(g2Id > 0);
        Assert.Equal(300, goal2.CalorieAdjustment);

        var goalHistory = await _dataAccess.GetGoalHistoryByUserIdAsync(userId);
        Assert.Equal(2, goalHistory.Count);

        var currentGoal = await _dataAccess.GetCurrentGoalByUserIdAsync(userId);
        Assert.NotNull(currentGoal);
        Assert.Equal(GoalType.WeightGain, currentGoal.Type);

        // Удаление цели
        Assert.True(await _dataAccess.DeleteGoalAsync(g2Id));
        var remainingGoals = await _dataAccess.GetGoalHistoryByUserIdAsync(userId);
        Assert.Single(remainingGoals);
    }

    [Fact]
    public async Task Requirement3And4_DailyLogs_Meals_MealItems_FullLifecycle()
    {
        var user = new User
        {
            Login = "dmitry_v",
            PasswordHash = "hash123",
            Name = "Дмитрий",
            Age = 32,
            Gender = Gender.Male,
            HeightCm = 178.0,
            WeightKg = 76.0,
            ActivityLevel = ActivityLevel.Moderate
        };
        int userId = await _dataAccess.CreateUserAsync(user);

        var today = DateTime.Today;
        var dailyLog = await _dataAccess.GetOrCreateDailyLogAsync(userId, today);
        Assert.True(dailyLog.Id > 0);

        // Повторный вызов GetOrCreateDailyLogAsync возвращает существующий лог
        var sameLog = await _dataAccess.GetOrCreateDailyLogAsync(userId, today);
        Assert.Equal(dailyLog.Id, sameLog.Id);

        // GetDailyLogAsync & GetDailyLogByIdAsync
        var fetchedByDate = await _dataAccess.GetDailyLogAsync(userId, today, true);
        Assert.NotNull(fetchedByDate);
        var fetchedById = await _dataAccess.GetDailyLogByIdAsync(dailyLog.Id, true);
        Assert.NotNull(fetchedById);

        var chicken = await _dataAccess.GetProductByBarcodeAsync("4607001000011");
        Assert.NotNull(chicken);

        // Создание Meal сразу со вложенными Items (включая новый продукт на лету)
        var breakfast = new Meal
        {
            DailyLogId = dailyLog.Id,
            Type = MealType.Breakfast,
            Time = new TimeSpan(8, 30, 0)
        };
        breakfast.AddItem(chicken, 200.0);
        breakfast.AddItem(new MealItem
        {
            Product = new Product
            {
                Name = "Домашний омлет",
                CaloriesPer100g = 180.0,
                ProteinPer100g = 12.0,
                FatPer100g = 14.0,
                CarbohydratesPer100g = 1.5,
                Barcode = "9990001112223"
            },
            AmountGrams = 150.0
        });

        int mealId = await _dataAccess.AddMealAsync(breakfast);
        Assert.True(mealId > 0);

        var loadedMeal = await _dataAccess.GetMealByIdAsync(mealId, true);
        Assert.NotNull(loadedMeal);
        Assert.Equal(2, loadedMeal.Items.Count);
        // 200g chicken (226 kcal) + 150g omelet (270 kcal) = 496 kcal
        Assert.Equal(496.0, loadedMeal.GetCalories());

        // Обновление порции продукта в приеме пищи
        var firstItem = loadedMeal.Items[0];
        firstItem.AmountGrams = 100.0;
        bool itemUpdated = await _dataAccess.UpdateMealItemAsync(firstItem);
        Assert.True(itemUpdated);

        var updatedItem = await _dataAccess.GetMealItemByIdAsync(firstItem.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(113.0, updatedItem.Calories);

        // Сводка по конкретному приему пищи
        var mealSummary = await _dataAccess.GetNutritionSummaryForMealAsync(mealId);
        Assert.Equal(383.0, mealSummary.TotalCalories); // 113 + 270

        // Удаление элемента приема пищи и самого приема пищи
        Assert.True(await _dataAccess.DeleteMealItemAsync(firstItem.Id));
        Assert.True(await _dataAccess.DeleteMealAsync(mealId));
        Assert.Null(await _dataAccess.GetMealByIdAsync(mealId));
    }

    [Fact]
    public async Task Requirement5And6_PeriodSummaries_MealTypeFilter_AndWaterIntake()
    {
        var user = new User
        {
            Login = "olga_s",
            PasswordHash = "hash123",
            Name = "Ольга",
            Age = 26,
            Gender = Gender.Female,
            HeightCm = 165.0,
            WeightKg = 58.0,
            ActivityLevel = ActivityLevel.Moderate
        };
        int userId = await _dataAccess.CreateUserAsync(user);

        var date1 = DateTime.Today.AddDays(-1);
        var date2 = DateTime.Today;

        var log1 = await _dataAccess.GetOrCreateDailyLogAsync(userId, date1);
        var log2 = await _dataAccess.GetOrCreateDailyLogAsync(userId, date2);

        var prod = new Product
        {
            Name = "Фитнес-батончик",
            CaloriesPer100g = 200.0,
            ProteinPer100g = 20.0,
            FatPer100g = 8.0,
            CarbohydratesPer100g = 12.0
        };
        int prodId = await _dataAccess.AddProductAsync(prod);

        // День 1: Завтрак (100г = 200 ккал) + Перекус (50г = 100 ккал)
        var b1 = new Meal { DailyLogId = log1.Id, Type = MealType.Breakfast, Time = new TimeSpan(8, 0, 0) };
        await _dataAccess.AddMealAsync(b1);
        await _dataAccess.AddMealItemAsync(new MealItem { MealId = b1.Id, ProductId = prodId, AmountGrams = 100.0 });

        var s1 = new Meal { DailyLogId = log1.Id, Type = MealType.Snack, Time = new TimeSpan(11, 0, 0) };
        await _dataAccess.AddMealAsync(s1);
        await _dataAccess.AddMealItemAsync(new MealItem { MealId = s1.Id, ProductId = prodId, AmountGrams = 50.0 });

        int w1Id = await _dataAccess.AddWaterIntakeAsync(new WaterIntake { DailyLogId = log1.Id, AmountMl = 500, Time = new TimeSpan(10, 0, 0) });

        // День 2: Обед (150г = 300 ккал)
        var l2 = new Meal { DailyLogId = log2.Id, Type = MealType.Lunch, Time = new TimeSpan(13, 0, 0) };
        await _dataAccess.AddMealAsync(l2);
        await _dataAccess.AddMealItemAsync(new MealItem { MealId = l2.Id, ProductId = prodId, AmountGrams = 150.0 });
        await _dataAccess.AddWaterIntakeAsync(new WaterIntake { DailyLogId = log2.Id, AmountMl = 800, Time = new TimeSpan(14, 0, 0) });

        // Получение всех логов за период
        var periodLogs = await _dataAccess.GetDailyLogsByPeriodAsync(userId, date1, date2, true);
        Assert.Equal(2, periodLogs.Count);

        // Полная сводка за 2 дня (200 + 100 + 300 = 600 ккал, вода 1300 мл)
        var totalSummary = await _dataAccess.GetNutritionSummaryForPeriodAsync(userId, date1, date2);
        Assert.Equal(600.0, totalSummary.TotalCalories);
        Assert.Equal(60.0, totalSummary.TotalProtein);
        Assert.Equal(1300, totalSummary.TotalWaterMl);

        // Сводка только по Завтракам и Перекусам (200 + 100 = 300 ккал)
        var filteredSummary = await _dataAccess.GetNutritionSummaryByMealTypesAsync(
            userId, date1, date2, new[] { MealType.Breakfast, MealType.Snack });
        Assert.Equal(300.0, filteredSummary.TotalCalories);

        // Удаление записи о воде
        Assert.True(await _dataAccess.DeleteWaterIntakeAsync(w1Id));
        Assert.Equal(0, await _dataAccess.GetTotalWaterMlByDateAsync(userId, date1));
    }

    [Fact]
    public async Task Requirement7_RecipesAndIngredients_Create_UpdateWithIngredients_AndDelete()
    {
        var user = new User
        {
            Login = "chef_user",
            PasswordHash = "hash123",
            Name = "Шеф Повар",
            Age = 35,
            Gender = Gender.Male,
            HeightCm = 180.0,
            WeightKg = 80.0,
            ActivityLevel = ActivityLevel.Moderate
        };
        int userId = await _dataAccess.CreateUserAsync(user);

        var rice = await _dataAccess.GetProductByBarcodeAsync("4607001000059");
        var salmon = await _dataAccess.GetProductByBarcodeAsync("4607001000097");
        Assert.NotNull(rice);
        Assert.NotNull(salmon);

        var recipe = new Recipe
        {
            UserId = userId,
            Name = "Боул с лососем",
            Servings = 2
        };
        recipe.AddIngredient(rice, 150.0);
        recipe.AddIngredient(salmon, 200.0);

        int recipeId = await _dataAccess.CreateRecipeAsync(recipe);
        Assert.True(recipeId > 0);

        // Добавление еще одного ингредиента отдельно
        var cucumber = await _dataAccess.GetProductByBarcodeAsync("4607001000158");
        Assert.NotNull(cucumber);
        var extraIng = new RecipeIngredient
        {
            RecipeId = recipeId,
            ProductId = cucumber.Id,
            AmountGrams = 100.0
        };
        int extraIngId = await _dataAccess.AddRecipeIngredientAsync(extraIng);
        Assert.True(extraIngId > 0);

        // Изменение граммовки отдельного ингредиента
        Assert.True(await _dataAccess.UpdateRecipeIngredientAmountAsync(extraIngId, 120.0));

        // Удаление отдельного ингредиента
        Assert.True(await _dataAccess.RemoveRecipeIngredientAsync(extraIngId));

        // Обновление рецепта целиком со списком ингредиентов
        recipe.Name = "Боул с лососем Премиум";
        recipe.Servings = 3;
        Assert.True(await _dataAccess.UpdateRecipeAsync(recipe));

        var userRecipes = await _dataAccess.GetRecipesByUserIdAsync(userId, true);
        Assert.Single(userRecipes);
        Assert.Equal("Боул с лососем Премиум", userRecipes[0].Name);
        Assert.Equal(3, userRecipes[0].Servings);
        Assert.Equal(2, userRecipes[0].Ingredients.Count);

        // Проверка что GetUserByIdAsync подгружает рецепты пользователя
        var fullUser = await _dataAccess.GetUserByIdAsync(userId);
        Assert.Single(fullUser!.Recipes);

        Assert.True(await _dataAccess.DeleteRecipeAsync(recipeId));
        Assert.Null(await _dataAccess.GetRecipeByIdAsync(recipeId));
    }

    [Fact]
    public async Task Requirement9_Products_Search_Update_DuplicateBarcodeDeduplication_AndDelete()
    {
        var allProducts = await _dataAccess.GetAllProductsAsync();
        Assert.True(allProducts.Count >= 16);

        // Пустой поисковый запрос возвращает все продукты
        var emptySearch = await _dataAccess.SearchProductsAsync("   ");
        Assert.Equal(allProducts.Count, emptySearch.Count);

        // Поиск по части названия
        var searchChicken = await _dataAccess.SearchProductsAsync("Куриное");
        Assert.NotEmpty(searchChicken);

        // Поиск по точному штрихкоду через SearchProductsAsync
        var searchByBarcode = await _dataAccess.SearchProductsAsync("4607001000042");
        Assert.NotEmpty(searchByBarcode);
        Assert.Equal("Творог 5%", searchByBarcode[0].Name);

        // Добавление продукта со штрихкодом
        var newProd = new Product
        {
            Name = "Йогурт Греческий Teos",
            CaloriesPer100g = 67.0,
            ProteinPer100g = 8.0,
            FatPer100g = 2.0,
            CarbohydratesPer100g = 4.2,
            Barcode = "4810268000015"
        };
        int prodId = await _dataAccess.AddProductAsync(newProd);
        Assert.True(prodId > 0);

        // Повторное добавление продукта с тем же штрихкодом возвращает существующий ID (без дублирования)
        var duplicateBarcodeProd = new Product
        {
            Name = "Йогурт Греческий Teos Дубль",
            CaloriesPer100g = 67.0,
            Barcode = "4810268000015"
        };
        int deduplicatedId = await _dataAccess.AddProductAsync(duplicateBarcodeProd);
        Assert.Equal(prodId, deduplicatedId);

        // Обновление продукта
        newProd.Name = "Йогурт Греческий 2%";
        newProd.CaloriesPer100g = 68.0;
        Assert.True(await _dataAccess.UpdateProductAsync(newProd));

        var updatedProd = await _dataAccess.GetProductByIdAsync(prodId);
        Assert.Equal("Йогурт Греческий 2%", updatedProd!.Name);
        Assert.Equal(68.0, updatedProd.CaloriesPer100g);

        // Удаление продукта
        Assert.True(await _dataAccess.DeleteProductAsync(prodId));
        Assert.Null(await _dataAccess.GetProductByIdAsync(prodId));
    }

    [Fact]
    public async Task EdgeCases_NullAndNotFoundBranches_WorkGracefully()
    {
        Assert.Null(await _dataAccess.GetProductByBarcodeAsync("   "));
        Assert.Null(await _dataAccess.GetProductByBarcodeAsync("0000000000000"));
        Assert.Null(await _dataAccess.GetUserByIdAsync(99999));
        Assert.Null(await _dataAccess.GetUserByLoginAsync("non_existent_login"));
        Assert.Null(await _dataAccess.GetDailyLogAsync(99999, DateTime.Today, false));
        Assert.Null(await _dataAccess.GetDailyLogByIdAsync(99999, false));
        Assert.Null(await _dataAccess.GetMealByIdAsync(99999, false));
        Assert.Null(await _dataAccess.GetRecipeByIdAsync(99999, false));

        var user = new User
        {
            Login = "minimal_user",
            PasswordHash = "pass",
            Name = "   ",
            Age = 0,
            HeightCm = 0,
            WeightKg = 0
        };
        int uid = await _dataAccess.CreateUserAsync(user);
        var loaded = await _dataAccess.GetUserByIdAsync(uid);
        Assert.NotNull(loaded);
        Assert.Equal("minimal_user", loaded.Name);

        var log = await _dataAccess.GetOrCreateDailyLogAsync(uid, DateTime.Today);
        var logNoDetails = await _dataAccess.GetDailyLogAsync(uid, DateTime.Today, false);
        Assert.NotNull(logNoDetails);
        var logByIdNoDetails = await _dataAccess.GetDailyLogByIdAsync(log.Id, false);
        Assert.NotNull(logByIdNoDetails);
        var periodNoDetails = await _dataAccess.GetDailyLogsByPeriodAsync(uid, DateTime.Today, DateTime.Today, false);
        Assert.Single(periodNoDetails);

        var meal = new Meal { DailyLogId = log.Id, Type = MealType.Lunch };
        int mealId = await _dataAccess.AddMealAsync(meal);
        var mealNoItems = await _dataAccess.GetMealByIdAsync(mealId, false);
        Assert.NotNull(mealNoItems);
        var mealsNoItems = await _dataAccess.GetMealsByDailyLogIdAsync(log.Id, false);
        Assert.Single(mealsNoItems);

        // Recipe with inline new Product (without ProductId)
        var recipe = new Recipe
        {
            UserId = uid,
            Name = "Тестовый салат",
            Servings = 1,
            Ingredients = new List<RecipeIngredient>
            {
                new()
                {
                    Product = new Product
                    {
                        Name = "Шпинат свежий",
                        CaloriesPer100g = 23,
                        ProteinPer100g = 2.9,
                        FatPer100g = 0.4,
                        CarbohydratesPer100g = 3.6
                    },
                    AmountGrams = 100
                }
            }
        };
        int rId = await _dataAccess.CreateRecipeAsync(recipe);
        var rNoIng = await _dataAccess.GetRecipeByIdAsync(rId, false);
        Assert.NotNull(rNoIng);
        var recipesNoIng = await _dataAccess.GetRecipesByUserIdAsync(uid, false);
        Assert.Single(recipesNoIng);
    }
}
