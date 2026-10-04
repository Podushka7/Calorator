using Calorator.Database.Contracts;
using Calorator.Database.Enums;
using Calorator.Database.Helpers;
using Calorator.Database.Models;
using Microsoft.Data.Sqlite;

namespace Calorator.Database.Implementation;

/// <summary>
/// Реализация модуля доступа к базе данных SQLite (компонент «Модуль доступа к базе данных» на диаграмме).
/// Предоставляет сервисам бизнес-логики методы работы с сущностями пользователей, дневника, рецептов и КБЖУ.
/// </summary>
public class SqliteDataAccess : IDataAccess
{
    public IDataBase Database { get; }

    public SqliteDataAccess(IDataBase database)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public SqliteDataAccess(string? connectionString = null)
        : this(new SqliteDatabase(connectionString))
    {
    }

    #region Пользователи (User) - Требование 1

    public async Task<int> CreateUserAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = @"
            INSERT INTO Users (Login, PasswordHash, Name, Age, Gender, HeightCm, WeightKg, ActivityLevel, CreatedAt)
            VALUES (@Login, @PasswordHash, @Name, @Age, @Gender, @HeightCm, @WeightKg, @ActivityLevel, @CreatedAt);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            user.Login,
            user.PasswordHash,
            user.Name,
            user.Age,
            user.Gender,
            user.HeightCm,
            user.WeightKg,
            user.ActivityLevel,
            CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }, null, ct);

        user.Id = (int)newId;
        return user.Id;
    }

    public async Task<User?> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Login, PasswordHash, Name, Age, Gender, HeightCm, WeightKg, ActivityLevel, CreatedAt
            FROM Users
            WHERE Id = @Id;
        ";

        var user = await Database.ExecuteSingleAsync(sql, MapUser, new { Id = id }, null, ct);
        if (user != null)
        {
            user.CurrentGoal = await GetCurrentGoalByUserIdAsync(user.Id, ct);
            user.CurrentNutritionNorm = await GetLatestNutritionNormByUserIdAsync(user.Id, ct);
        }
        return user;
    }

    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);

        const string sql = @"
            SELECT Id, Login, PasswordHash, Name, Age, Gender, HeightCm, WeightKg, ActivityLevel, CreatedAt
            FROM Users
            WHERE Login = @Login COLLATE NOCASE;
        ";

        var user = await Database.ExecuteSingleAsync(sql, MapUser, new { Login = login.Trim() }, null, ct);
        if (user != null)
        {
            user.CurrentGoal = await GetCurrentGoalByUserIdAsync(user.Id, ct);
            user.CurrentNutritionNorm = await GetLatestNutritionNormByUserIdAsync(user.Id, ct);
        }
        return user;
    }

    public async Task<bool> UpdateUserProfileAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = @"
            UPDATE Users
            SET Name = @Name,
                Age = @Age,
                Gender = @Gender,
                HeightCm = @HeightCm,
                WeightKg = @WeightKg,
                ActivityLevel = @ActivityLevel
            WHERE Id = @Id;
        ";

        int rows = await Database.ExecuteNonQueryAsync(sql, new
        {
            user.Id,
            user.Name,
            user.Age,
            user.Gender,
            user.HeightCm,
            user.WeightKg,
            user.ActivityLevel
        }, null, ct);

        return rows > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);

        const string sql = @"
            UPDATE Users
            SET PasswordHash = @PasswordHash
            WHERE Id = @Id;
        ";

        int rows = await Database.ExecuteNonQueryAsync(sql, new
        {
            Id = userId,
            PasswordHash = newPasswordHash
        }, null, ct);

        return rows > 0;
    }

    public async Task<bool> DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Users WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = userId }, null, ct);
        return rows > 0;
    }

    public async Task<bool> UserExistsAsync(string login, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login)) return false;

        const string sql = "SELECT COUNT(1) FROM Users WHERE Login = @Login COLLATE NOCASE;";
        long count = await Database.ExecuteScalarAsync<long>(sql, new { Login = login.Trim() }, null, ct);
        return count > 0;
    }

    private static User MapUser(SqliteDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32Safe("Id"),
            Login = reader.GetStringSafe("Login"),
            PasswordHash = reader.GetStringSafe("PasswordHash"),
            Name = reader.GetStringSafe("Name"),
            Age = reader.GetInt32Safe("Age"),
            Gender = reader.GetEnumSafe<Gender>("Gender"),
            HeightCm = reader.GetDoubleSafe("HeightCm"),
            WeightKg = reader.GetDoubleSafe("WeightKg"),
            ActivityLevel = reader.GetEnumSafe<ActivityLevel>("ActivityLevel"),
            CreatedAt = reader.GetDateTimeSafe("CreatedAt")
        };
    }

    #endregion

    #region Нормы КБЖУ (NutritionNorm) - Требование 2

    public async Task<int> SaveNutritionNormAsync(NutritionNorm norm, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(norm);

        const string sql = @"
            INSERT INTO NutritionNorms (UserId, Calories, Protein, Fat, Carbohydrates, CalculatedAt)
            VALUES (@UserId, @Calories, @Protein, @Fat, @Carbohydrates, @CalculatedAt);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            norm.UserId,
            norm.Calories,
            norm.Protein,
            norm.Fat,
            norm.Carbohydrates,
            CalculatedAt = norm.CalculatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }, null, ct);

        norm.Id = (int)newId;
        return norm.Id;
    }

    public async Task<NutritionNorm?> GetLatestNutritionNormByUserIdAsync(int userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Calories, Protein, Fat, Carbohydrates, CalculatedAt
            FROM NutritionNorms
            WHERE UserId = @UserId
            ORDER BY Id DESC
            LIMIT 1;
        ";

        return await Database.ExecuteSingleAsync(sql, MapNutritionNorm, new { UserId = userId }, null, ct);
    }

    public async Task<IReadOnlyList<NutritionNorm>> GetNutritionNormHistoryByUserIdAsync(int userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Calories, Protein, Fat, Carbohydrates, CalculatedAt
            FROM NutritionNorms
            WHERE UserId = @UserId
            ORDER BY Id DESC;
        ";

        return await Database.ExecuteQueryAsync(sql, MapNutritionNorm, new { UserId = userId }, null, ct);
    }

    private static NutritionNorm MapNutritionNorm(SqliteDataReader reader)
    {
        return new NutritionNorm
        {
            Id = reader.GetInt32Safe("Id"),
            UserId = reader.GetInt32Safe("UserId"),
            Calories = reader.GetDoubleSafe("Calories"),
            Protein = reader.GetDoubleSafe("Protein"),
            Fat = reader.GetDoubleSafe("Fat"),
            Carbohydrates = reader.GetDoubleSafe("Carbohydrates"),
            CalculatedAt = reader.GetDateTimeSafe("CalculatedAt")
        };
    }

    #endregion

    #region Цели пользователя (Goal) - Требование 8

    public async Task<int> SetGoalAsync(Goal goal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        const string sql = @"
            INSERT INTO Goals (UserId, Type, TargetWeightKg, CalorieAdjustment, CreatedAt)
            VALUES (@UserId, @Type, @TargetWeightKg, @CalorieAdjustment, @CreatedAt);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            goal.UserId,
            goal.Type,
            goal.TargetWeightKg,
            goal.CalorieAdjustment,
            CreatedAt = goal.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }, null, ct);

        goal.Id = (int)newId;
        return goal.Id;
    }

    public async Task<Goal?> GetCurrentGoalByUserIdAsync(int userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Type, TargetWeightKg, CalorieAdjustment, CreatedAt
            FROM Goals
            WHERE UserId = @UserId
            ORDER BY Id DESC
            LIMIT 1;
        ";

        return await Database.ExecuteSingleAsync(sql, MapGoal, new { UserId = userId }, null, ct);
    }

    public async Task<IReadOnlyList<Goal>> GetGoalHistoryByUserIdAsync(int userId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Type, TargetWeightKg, CalorieAdjustment, CreatedAt
            FROM Goals
            WHERE UserId = @UserId
            ORDER BY Id DESC;
        ";

        return await Database.ExecuteQueryAsync(sql, MapGoal, new { UserId = userId }, null, ct);
    }

    public async Task<bool> DeleteGoalAsync(int goalId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Goals WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = goalId }, null, ct);
        return rows > 0;
    }

    private static Goal MapGoal(SqliteDataReader reader)
    {
        return new Goal
        {
            Id = reader.GetInt32Safe("Id"),
            UserId = reader.GetInt32Safe("UserId"),
            Type = reader.GetEnumSafe<GoalType>("Type"),
            TargetWeightKg = reader.GetDoubleSafe("TargetWeightKg"),
            CalorieAdjustment = reader.GetInt32Safe("CalorieAdjustment"),
            CreatedAt = reader.GetDateTimeSafe("CreatedAt")
        };
    }

    #endregion

    #region Продукты и штрихкоды (Product) - Требования 4, 7, 9

    public async Task<int> AddProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        const string sql = @"
            INSERT INTO Products (Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g, Barcode)
            VALUES (@Name, @CaloriesPer100g, @ProteinPer100g, @FatPer100g, @CarbohydratesPer100g, @Barcode);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            product.Name,
            product.CaloriesPer100g,
            product.ProteinPer100g,
            product.FatPer100g,
            product.CarbohydratesPer100g,
            Barcode = string.IsNullOrWhiteSpace(product.Barcode) ? null : product.Barcode.Trim()
        }, null, ct);

        product.Id = (int)newId;
        return product.Id;
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g, Barcode
            FROM Products
            WHERE Id = @Id;
        ";

        return await Database.ExecuteSingleAsync(sql, MapProduct, new { Id = id }, null, ct);
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        const string sql = @"
            SELECT Id, Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g, Barcode
            FROM Products
            WHERE Barcode = @Barcode;
        ";

        return await Database.ExecuteSingleAsync(sql, MapProduct, new { Barcode = barcode.Trim() }, null, ct);
    }

    public async Task<IReadOnlyList<Product>> SearchProductsAsync(string query, int limit = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllProductsAsync(ct);
        }

        const string sql = @"
            SELECT Id, Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g, Barcode
            FROM Products
            WHERE Name LIKE @LikePattern OR Barcode = @ExactQuery
            ORDER BY 
                CASE WHEN Barcode = @ExactQuery THEN 0
                     WHEN Name LIKE @ExactNameStart THEN 1
                     ELSE 2 END,
                Name ASC
            LIMIT @Limit;
        ";

        string trimmed = query.Trim();
        return await Database.ExecuteQueryAsync(sql, MapProduct, new
        {
            LikePattern = $"%{trimmed}%",
            ExactNameStart = $"{trimmed}%",
            ExactQuery = trimmed,
            Limit = Math.Max(1, limit)
        }, null, ct);
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g, Barcode
            FROM Products
            ORDER BY Name ASC;
        ";

        return await Database.ExecuteQueryAsync(sql, MapProduct, null, null, ct);
    }

    public async Task<bool> UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        const string sql = @"
            UPDATE Products
            SET Name = @Name,
                CaloriesPer100g = @CaloriesPer100g,
                ProteinPer100g = @ProteinPer100g,
                FatPer100g = @FatPer100g,
                CarbohydratesPer100g = @CarbohydratesPer100g,
                Barcode = @Barcode
            WHERE Id = @Id;
        ";

        int rows = await Database.ExecuteNonQueryAsync(sql, new
        {
            product.Id,
            product.Name,
            product.CaloriesPer100g,
            product.ProteinPer100g,
            product.FatPer100g,
            product.CarbohydratesPer100g,
            Barcode = string.IsNullOrWhiteSpace(product.Barcode) ? null : product.Barcode.Trim()
        }, null, ct);

        return rows > 0;
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Products WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = id }, null, ct);
        return rows > 0;
    }

    private static Product MapProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32Safe("Id"),
            Name = reader.GetStringSafe("Name"),
            CaloriesPer100g = reader.GetDoubleSafe("CaloriesPer100g"),
            ProteinPer100g = reader.GetDoubleSafe("ProteinPer100g"),
            FatPer100g = reader.GetDoubleSafe("FatPer100g"),
            CarbohydratesPer100g = reader.GetDoubleSafe("CarbohydratesPer100g"),
            Barcode = reader.GetNullableStringSafe("Barcode")
        };
    }

    #endregion

    #region Дневник питания и приемы пищи (DailyLog & Meal) - Требования 3, 4

    public async Task<DailyLog> GetOrCreateDailyLogAsync(int userId, DateTime date, CancellationToken ct = default)
    {
        string dateStr = date.ToString("yyyy-MM-dd");

        const string selectSql = @"
            SELECT Id, UserId, Date
            FROM DailyLogs
            WHERE UserId = @UserId AND Date = @Date;
        ";

        var log = await Database.ExecuteSingleAsync(selectSql, MapDailyLog, new
        {
            UserId = userId,
            Date = dateStr
        }, null, ct);

        if (log == null)
        {
            const string insertSql = @"
                INSERT INTO DailyLogs (UserId, Date)
                VALUES (@UserId, @Date);
                SELECT last_insert_rowid();
            ";

            long newId = await Database.ExecuteScalarAsync<long>(insertSql, new
            {
                UserId = userId,
                Date = dateStr
            }, null, ct);

            log = new DailyLog
            {
                Id = (int)newId,
                UserId = userId,
                Date = date.Date
            };
        }
        else
        {
            log.Meals = (await GetMealsByDailyLogIdAsync(log.Id, true, ct)).ToList();
            log.WaterIntakes = (await GetWaterIntakesByDailyLogIdAsync(log.Id, ct)).ToList();
        }

        return log;
    }

    public async Task<DailyLog?> GetDailyLogAsync(int userId, DateTime date, bool includeDetails = true, CancellationToken ct = default)
    {
        string dateStr = date.ToString("yyyy-MM-dd");

        const string sql = @"
            SELECT Id, UserId, Date
            FROM DailyLogs
            WHERE UserId = @UserId AND Date = @Date;
        ";

        var log = await Database.ExecuteSingleAsync(sql, MapDailyLog, new
        {
            UserId = userId,
            Date = dateStr
        }, null, ct);

        if (log != null && includeDetails)
        {
            log.Meals = (await GetMealsByDailyLogIdAsync(log.Id, true, ct)).ToList();
            log.WaterIntakes = (await GetWaterIntakesByDailyLogIdAsync(log.Id, ct)).ToList();
        }

        return log;
    }

    public async Task<DailyLog?> GetDailyLogByIdAsync(int id, bool includeDetails = true, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Date
            FROM DailyLogs
            WHERE Id = @Id;
        ";

        var log = await Database.ExecuteSingleAsync(sql, MapDailyLog, new { Id = id }, null, ct);
        if (log != null && includeDetails)
        {
            log.Meals = (await GetMealsByDailyLogIdAsync(log.Id, true, ct)).ToList();
            log.WaterIntakes = (await GetWaterIntakesByDailyLogIdAsync(log.Id, ct)).ToList();
        }
        return log;
    }

    public async Task<IReadOnlyList<DailyLog>> GetDailyLogsByPeriodAsync(
        int userId,
        DateTime startDate,
        DateTime endDate,
        bool includeDetails = true,
        CancellationToken ct = default)
    {
        string startStr = startDate.ToString("yyyy-MM-dd");
        string endStr = endDate.ToString("yyyy-MM-dd");

        const string sql = @"
            SELECT Id, UserId, Date
            FROM DailyLogs
            WHERE UserId = @UserId AND Date BETWEEN @StartDate AND @EndDate
            ORDER BY Date ASC;
        ";

        var logs = await Database.ExecuteQueryAsync(sql, MapDailyLog, new
        {
            UserId = userId,
            StartDate = startStr,
            EndDate = endStr
        }, null, ct);

        if (includeDetails)
        {
            foreach (var log in logs)
            {
                log.Meals = (await GetMealsByDailyLogIdAsync(log.Id, true, ct)).ToList();
                log.WaterIntakes = (await GetWaterIntakesByDailyLogIdAsync(log.Id, ct)).ToList();
            }
        }

        return logs;
    }

    public async Task<int> AddMealAsync(Meal meal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(meal);

        return await Database.ExecuteInTransactionAsync(async transaction =>
        {
            const string insertMealSql = @"
                INSERT INTO Meals (DailyLogId, Type, Time)
                VALUES (@DailyLogId, @Type, @Time);
                SELECT last_insert_rowid();
            ";

            long mealId = await Database.ExecuteScalarAsync<long>(insertMealSql, new
            {
                meal.DailyLogId,
                meal.Type,
                Time = meal.Time.ToString(@"hh\:mm\:ss")
            }, transaction, ct);

            meal.Id = (int)mealId;

            if (meal.Items.Count > 0)
            {
                foreach (var item in meal.Items)
                {
                    item.MealId = meal.Id;
                    await AddMealItemInternalAsync(item, transaction, ct);
                }
            }

            return meal.Id;
        }, ct);
    }

    public async Task<Meal?> GetMealByIdAsync(int id, bool includeItems = true, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, DailyLogId, Type, Time
            FROM Meals
            WHERE Id = @Id;
        ";

        var meal = await Database.ExecuteSingleAsync(sql, MapMeal, new { Id = id }, null, ct);
        if (meal != null && includeItems)
        {
            meal.Items = (await GetMealItemsByMealIdAsync(meal.Id, ct)).ToList();
        }
        return meal;
    }

    public async Task<IReadOnlyList<Meal>> GetMealsByDailyLogIdAsync(int dailyLogId, bool includeItems = true, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, DailyLogId, Type, Time
            FROM Meals
            WHERE DailyLogId = @DailyLogId
            ORDER BY Time ASC, Id ASC;
        ";

        var meals = await Database.ExecuteQueryAsync(sql, MapMeal, new { DailyLogId = dailyLogId }, null, ct);
        if (includeItems)
        {
            foreach (var meal in meals)
            {
                meal.Items = (await GetMealItemsByMealIdAsync(meal.Id, ct)).ToList();
            }
        }
        return meals;
    }

    public async Task<bool> DeleteMealAsync(int mealId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Meals WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = mealId }, null, ct);
        return rows > 0;
    }

    private static DailyLog MapDailyLog(SqliteDataReader reader)
    {
        return new DailyLog
        {
            Id = reader.GetInt32Safe("Id"),
            UserId = reader.GetInt32Safe("UserId"),
            Date = reader.GetDateTimeSafe("Date")
        };
    }

    private static Meal MapMeal(SqliteDataReader reader)
    {
        return new Meal
        {
            Id = reader.GetInt32Safe("Id"),
            DailyLogId = reader.GetInt32Safe("DailyLogId"),
            Type = reader.GetEnumSafe<MealType>("Type"),
            Time = reader.GetTimeSpanSafe("Time")
        };
    }

    #endregion

    #region Элементы приема пищи (MealItem) - Требование 4

    public async Task<int> AddMealItemAsync(MealItem item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        return await Database.ExecuteInTransactionAsync(async transaction =>
        {
            return await AddMealItemInternalAsync(item, transaction, ct);
        }, ct);
    }

    private async Task<int> AddMealItemInternalAsync(MealItem item, SqliteTransaction? transaction, CancellationToken ct)
    {
        if (item.Product == null && (item.Calories == 0 && item.Protein == 0 && item.Fat == 0 && item.Carbohydrates == 0))
        {
            item.Product = await GetProductByIdAsync(item.ProductId, ct);
            item.CalculateNutrition();
        }

        const string sql = @"
            INSERT INTO MealItems (MealId, ProductId, AmountGrams, Calories, Protein, Fat, Carbohydrates)
            VALUES (@MealId, @ProductId, @AmountGrams, @Calories, @Protein, @Fat, @Carbohydrates);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            item.MealId,
            item.ProductId,
            item.AmountGrams,
            item.Calories,
            item.Protein,
            item.Fat,
            item.Carbohydrates
        }, transaction, ct);

        item.Id = (int)newId;
        return item.Id;
    }

    public async Task<bool> UpdateMealItemAsync(MealItem item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Product == null)
        {
            item.Product = await GetProductByIdAsync(item.ProductId, ct);
        }
        item.CalculateNutrition();

        const string sql = @"
            UPDATE MealItems
            SET AmountGrams = @AmountGrams,
                Calories = @Calories,
                Protein = @Protein,
                Fat = @Fat,
                Carbohydrates = @Carbohydrates
            WHERE Id = @Id;
        ";

        int rows = await Database.ExecuteNonQueryAsync(sql, new
        {
            item.Id,
            item.AmountGrams,
            item.Calories,
            item.Protein,
            item.Fat,
            item.Carbohydrates
        }, null, ct);

        return rows > 0;
    }

    public async Task<bool> DeleteMealItemAsync(int itemId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM MealItems WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = itemId }, null, ct);
        return rows > 0;
    }

    public async Task<MealItem?> GetMealItemByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                mi.Id, mi.MealId, mi.ProductId, mi.AmountGrams, mi.Calories, mi.Protein, mi.Fat, mi.Carbohydrates,
                p.Id AS P_Id, p.Name AS P_Name, p.CaloriesPer100g AS P_Cal, p.ProteinPer100g AS P_Prot,
                p.FatPer100g AS P_Fat, p.CarbohydratesPer100g AS P_Carb, p.Barcode AS P_Barcode
            FROM MealItems mi
            INNER JOIN Products p ON mi.ProductId = p.Id
            WHERE mi.Id = @Id;
        ";

        return await Database.ExecuteSingleAsync(sql, MapMealItemWithProduct, new { Id = id }, null, ct);
    }

    private async Task<IReadOnlyList<MealItem>> GetMealItemsByMealIdAsync(int mealId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                mi.Id, mi.MealId, mi.ProductId, mi.AmountGrams, mi.Calories, mi.Protein, mi.Fat, mi.Carbohydrates,
                p.Id AS P_Id, p.Name AS P_Name, p.CaloriesPer100g AS P_Cal, p.ProteinPer100g AS P_Prot,
                p.FatPer100g AS P_Fat, p.CarbohydratesPer100g AS P_Carb, p.Barcode AS P_Barcode
            FROM MealItems mi
            INNER JOIN Products p ON mi.ProductId = p.Id
            WHERE mi.MealId = @MealId
            ORDER BY mi.Id ASC;
        ";

        return await Database.ExecuteQueryAsync(sql, MapMealItemWithProduct, new { MealId = mealId }, null, ct);
    }

    private static MealItem MapMealItemWithProduct(SqliteDataReader reader)
    {
        var product = new Product
        {
            Id = reader.GetInt32Safe("P_Id"),
            Name = reader.GetStringSafe("P_Name"),
            CaloriesPer100g = reader.GetDoubleSafe("P_Cal"),
            ProteinPer100g = reader.GetDoubleSafe("P_Prot"),
            FatPer100g = reader.GetDoubleSafe("P_Fat"),
            CarbohydratesPer100g = reader.GetDoubleSafe("P_Carb"),
            Barcode = reader.GetNullableStringSafe("P_Barcode")
        };

        return new MealItem
        {
            Id = reader.GetInt32Safe("Id"),
            MealId = reader.GetInt32Safe("MealId"),
            ProductId = reader.GetInt32Safe("ProductId"),
            Product = product,
            AmountGrams = reader.GetDoubleSafe("AmountGrams"),
            Calories = reader.GetDoubleSafe("Calories"),
            Protein = reader.GetDoubleSafe("Protein"),
            Fat = reader.GetDoubleSafe("Fat"),
            Carbohydrates = reader.GetDoubleSafe("Carbohydrates")
        };
    }

    #endregion

    #region Учет воды (WaterIntake) - Требование 6

    public async Task<int> AddWaterIntakeAsync(WaterIntake water, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(water);

        const string sql = @"
            INSERT INTO WaterIntakes (DailyLogId, AmountMl, Time)
            VALUES (@DailyLogId, @AmountMl, @Time);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            water.DailyLogId,
            water.AmountMl,
            Time = water.Time.ToString(@"hh\:mm\:ss")
        }, null, ct);

        water.Id = (int)newId;
        return water.Id;
    }

    public async Task<bool> DeleteWaterIntakeAsync(int waterIntakeId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM WaterIntakes WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = waterIntakeId }, null, ct);
        return rows > 0;
    }

    public async Task<IReadOnlyList<WaterIntake>> GetWaterIntakesByDailyLogIdAsync(int dailyLogId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, DailyLogId, AmountMl, Time
            FROM WaterIntakes
            WHERE DailyLogId = @DailyLogId
            ORDER BY Time ASC, Id ASC;
        ";

        return await Database.ExecuteQueryAsync(sql, MapWaterIntake, new { DailyLogId = dailyLogId }, null, ct);
    }

    public async Task<int> GetTotalWaterMlByDateAsync(int userId, DateTime date, CancellationToken ct = default)
    {
        string dateStr = date.ToString("yyyy-MM-dd");

        const string sql = @"
            SELECT IFNULL(SUM(w.AmountMl), 0)
            FROM WaterIntakes w
            INNER JOIN DailyLogs d ON w.DailyLogId = d.Id
            WHERE d.UserId = @UserId AND d.Date = @Date;
        ";

        long total = await Database.ExecuteScalarAsync<long>(sql, new
        {
            UserId = userId,
            Date = dateStr
        }, null, ct);

        return (int)total;
    }

    private static WaterIntake MapWaterIntake(SqliteDataReader reader)
    {
        return new WaterIntake
        {
            Id = reader.GetInt32Safe("Id"),
            DailyLogId = reader.GetInt32Safe("DailyLogId"),
            AmountMl = reader.GetInt32Safe("AmountMl"),
            Time = reader.GetTimeSpanSafe("Time")
        };
    }

    #endregion

    #region Рецепты (Recipe & RecipeIngredient) - Требование 7

    public async Task<int> CreateRecipeAsync(Recipe recipe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return await Database.ExecuteInTransactionAsync(async transaction =>
        {
            const string insertRecipeSql = @"
                INSERT INTO Recipes (UserId, Name, Servings)
                VALUES (@UserId, @Name, @Servings);
                SELECT last_insert_rowid();
            ";

            long recipeId = await Database.ExecuteScalarAsync<long>(insertRecipeSql, new
            {
                recipe.UserId,
                recipe.Name,
                recipe.Servings
            }, transaction, ct);

            recipe.Id = (int)recipeId;

            if (recipe.Ingredients.Count > 0)
            {
                foreach (var ing in recipe.Ingredients)
                {
                    ing.RecipeId = recipe.Id;
                    const string insertIngSql = @"
                        INSERT INTO RecipeIngredients (RecipeId, ProductId, AmountGrams)
                        VALUES (@RecipeId, @ProductId, @AmountGrams);
                        SELECT last_insert_rowid();
                    ";

                    long ingId = await Database.ExecuteScalarAsync<long>(insertIngSql, new
                    {
                        ing.RecipeId,
                        ing.ProductId,
                        ing.AmountGrams
                    }, transaction, ct);

                    ing.Id = (int)ingId;
                }
            }

            return recipe.Id;
        }, ct);
    }

    public async Task<Recipe?> GetRecipeByIdAsync(int id, bool includeIngredients = true, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Name, Servings
            FROM Recipes
            WHERE Id = @Id;
        ";

        var recipe = await Database.ExecuteSingleAsync(sql, MapRecipe, new { Id = id }, null, ct);
        if (recipe != null && includeIngredients)
        {
            recipe.Ingredients = (await GetRecipeIngredientsAsync(recipe.Id, ct)).ToList();
        }
        return recipe;
    }

    public async Task<IReadOnlyList<Recipe>> GetRecipesByUserIdAsync(int userId, bool includeIngredients = true, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserId, Name, Servings
            FROM Recipes
            WHERE UserId = @UserId
            ORDER BY Name ASC;
        ";

        var recipes = await Database.ExecuteQueryAsync(sql, MapRecipe, new { UserId = userId }, null, ct);
        if (includeIngredients)
        {
            foreach (var r in recipes)
            {
                r.Ingredients = (await GetRecipeIngredientsAsync(r.Id, ct)).ToList();
            }
        }
        return recipes;
    }

    public async Task<bool> UpdateRecipeAsync(Recipe recipe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        const string sql = @"
            UPDATE Recipes
            SET Name = @Name,
                Servings = @Servings
            WHERE Id = @Id;
        ";

        int rows = await Database.ExecuteNonQueryAsync(sql, new
        {
            recipe.Id,
            recipe.Name,
            recipe.Servings
        }, null, ct);

        return rows > 0;
    }

    public async Task<bool> DeleteRecipeAsync(int recipeId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Recipes WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = recipeId }, null, ct);
        return rows > 0;
    }

    public async Task<int> AddRecipeIngredientAsync(RecipeIngredient ingredient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        const string sql = @"
            INSERT INTO RecipeIngredients (RecipeId, ProductId, AmountGrams)
            VALUES (@RecipeId, @ProductId, @AmountGrams);
            SELECT last_insert_rowid();
        ";

        long newId = await Database.ExecuteScalarAsync<long>(sql, new
        {
            ingredient.RecipeId,
            ingredient.ProductId,
            ingredient.AmountGrams
        }, null, ct);

        ingredient.Id = (int)newId;
        return ingredient.Id;
    }

    public async Task<bool> UpdateRecipeIngredientAmountAsync(int ingredientId, double amountGrams, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE RecipeIngredients
            SET AmountGrams = @AmountGrams
            WHERE Id = @Id;
        ";

        int rows = await Database.ExecuteNonQueryAsync(sql, new
        {
            Id = ingredientId,
            AmountGrams = amountGrams
        }, null, ct);

        return rows > 0;
    }

    public async Task<bool> RemoveRecipeIngredientAsync(int ingredientId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM RecipeIngredients WHERE Id = @Id;";
        int rows = await Database.ExecuteNonQueryAsync(sql, new { Id = ingredientId }, null, ct);
        return rows > 0;
    }

    public async Task<IReadOnlyList<RecipeIngredient>> GetRecipeIngredientsAsync(int recipeId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                ri.Id, ri.RecipeId, ri.ProductId, ri.AmountGrams,
                p.Id AS P_Id, p.Name AS P_Name, p.CaloriesPer100g AS P_Cal, p.ProteinPer100g AS P_Prot,
                p.FatPer100g AS P_Fat, p.CarbohydratesPer100g AS P_Carb, p.Barcode AS P_Barcode
            FROM RecipeIngredients ri
            INNER JOIN Products p ON ri.ProductId = p.Id
            WHERE ri.RecipeId = @RecipeId
            ORDER BY ri.Id ASC;
        ";

        return await Database.ExecuteQueryAsync(sql, MapRecipeIngredientWithProduct, new { RecipeId = recipeId }, null, ct);
    }

    private static Recipe MapRecipe(SqliteDataReader reader)
    {
        return new Recipe
        {
            Id = reader.GetInt32Safe("Id"),
            UserId = reader.GetInt32Safe("UserId"),
            Name = reader.GetStringSafe("Name"),
            Servings = reader.GetInt32Safe("Servings")
        };
    }

    private static RecipeIngredient MapRecipeIngredientWithProduct(SqliteDataReader reader)
    {
        var product = new Product
        {
            Id = reader.GetInt32Safe("P_Id"),
            Name = reader.GetStringSafe("P_Name"),
            CaloriesPer100g = reader.GetDoubleSafe("P_Cal"),
            ProteinPer100g = reader.GetDoubleSafe("P_Prot"),
            FatPer100g = reader.GetDoubleSafe("P_Fat"),
            CarbohydratesPer100g = reader.GetDoubleSafe("P_Carb"),
            Barcode = reader.GetNullableStringSafe("P_Barcode")
        };

        return new RecipeIngredient
        {
            Id = reader.GetInt32Safe("Id"),
            RecipeId = reader.GetInt32Safe("RecipeId"),
            ProductId = reader.GetInt32Safe("ProductId"),
            Product = product,
            AmountGrams = reader.GetDoubleSafe("AmountGrams")
        };
    }

    #endregion

    #region Расчет КБЖУ за период и приемы пищи - Требование 5

    public async Task<NutritionSummary> GetNutritionSummaryForPeriodAsync(
        int userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        string startStr = startDate.ToString("yyyy-MM-dd");
        string endStr = endDate.ToString("yyyy-MM-dd");

        const string nutritionSql = @"
            SELECT 
                IFNULL(SUM(mi.Calories), 0) AS TotalCal,
                IFNULL(SUM(mi.Protein), 0) AS TotalProt,
                IFNULL(SUM(mi.Fat), 0) AS TotalFat,
                IFNULL(SUM(mi.Carbohydrates), 0) AS TotalCarb,
                COUNT(DISTINCT d.Date) AS ActiveDaysCount
            FROM DailyLogs d
            LEFT JOIN Meals m ON m.DailyLogId = d.Id
            LEFT JOIN MealItems mi ON mi.MealId = m.Id
            WHERE d.UserId = @UserId AND d.Date BETWEEN @StartDate AND @EndDate;
        ";

        const string waterSql = @"
            SELECT IFNULL(SUM(w.AmountMl), 0) AS TotalWater
            FROM DailyLogs d
            INNER JOIN WaterIntakes w ON w.DailyLogId = d.Id
            WHERE d.UserId = @UserId AND d.Date BETWEEN @StartDate AND @EndDate;
        ";

        var summary = await Database.ExecuteSingleAsync(nutritionSql, reader => new NutritionSummary
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalCalories = Math.Round(reader.GetDoubleSafe("TotalCal"), 2),
            TotalProtein = Math.Round(reader.GetDoubleSafe("TotalProt"), 2),
            TotalFat = Math.Round(reader.GetDoubleSafe("TotalFat"), 2),
            TotalCarbohydrates = Math.Round(reader.GetDoubleSafe("TotalCarb"), 2),
            DaysCount = reader.GetInt32Safe("ActiveDaysCount")
        }, new
        {
            UserId = userId,
            StartDate = startStr,
            EndDate = endStr
        }, null, ct) ?? new NutritionSummary();

        long totalWater = await Database.ExecuteScalarAsync<long>(waterSql, new
        {
            UserId = userId,
            StartDate = startStr,
            EndDate = endStr
        }, null, ct);

        summary.TotalWaterMl = (int)totalWater;
        return summary;
    }

    public async Task<NutritionSummary> GetNutritionSummaryForMealAsync(int mealId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                IFNULL(SUM(Calories), 0) AS TotalCal,
                IFNULL(SUM(Protein), 0) AS TotalProt,
                IFNULL(SUM(Fat), 0) AS TotalFat,
                IFNULL(SUM(Carbohydrates), 0) AS TotalCarb,
                COUNT(1) AS ItemsCount
            FROM MealItems
            WHERE MealId = @MealId;
        ";

        return await Database.ExecuteSingleAsync(sql, reader => new NutritionSummary
        {
            TotalCalories = Math.Round(reader.GetDoubleSafe("TotalCal"), 2),
            TotalProtein = Math.Round(reader.GetDoubleSafe("TotalProt"), 2),
            TotalFat = Math.Round(reader.GetDoubleSafe("TotalFat"), 2),
            TotalCarbohydrates = Math.Round(reader.GetDoubleSafe("TotalCarb"), 2),
            DaysCount = 1
        }, new { MealId = mealId }, null, ct) ?? new NutritionSummary();
    }

    #endregion
}
