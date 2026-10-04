using Microsoft.Data.Sqlite;

namespace Calorator.Database.Implementation;

/// <summary>
/// Инициализатор базы данных SQLite: создает таблицы, индексы, настраивает внешние ключи и начальные данные.
/// </summary>
public static class DatabaseInitializer
{
    private const string SchemaSql = @"
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Login TEXT NOT NULL UNIQUE COLLATE NOCASE,
            PasswordHash TEXT NOT NULL,
            Name TEXT NOT NULL,
            Age INTEGER NOT NULL,
            Gender INTEGER NOT NULL,
            HeightCm REAL NOT NULL,
            WeightKg REAL NOT NULL,
            ActivityLevel INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_Users_Login ON Users(Login);

        CREATE TABLE IF NOT EXISTS NutritionNorms (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Calories REAL NOT NULL,
            Protein REAL NOT NULL,
            Fat REAL NOT NULL,
            Carbohydrates REAL NOT NULL,
            CalculatedAt TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_NutritionNorms_UserId ON NutritionNorms(UserId);

        CREATE TABLE IF NOT EXISTS Goals (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Type INTEGER NOT NULL,
            TargetWeightKg REAL NOT NULL,
            CalorieAdjustment INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_Goals_UserId ON Goals(UserId);

        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL COLLATE NOCASE,
            CaloriesPer100g REAL NOT NULL,
            ProteinPer100g REAL NOT NULL,
            FatPer100g REAL NOT NULL,
            CarbohydratesPer100g REAL NOT NULL,
            Barcode TEXT UNIQUE
        );

        CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);
        CREATE INDEX IF NOT EXISTS IX_Products_Barcode ON Products(Barcode);

        CREATE TABLE IF NOT EXISTS DailyLogs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Date TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
            UNIQUE(UserId, Date)
        );

        CREATE INDEX IF NOT EXISTS IX_DailyLogs_UserId_Date ON DailyLogs(UserId, Date);

        CREATE TABLE IF NOT EXISTS Meals (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DailyLogId INTEGER NOT NULL,
            Type INTEGER NOT NULL,
            Time TEXT NOT NULL,
            FOREIGN KEY (DailyLogId) REFERENCES DailyLogs(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_Meals_DailyLogId ON Meals(DailyLogId);

        CREATE TABLE IF NOT EXISTS MealItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            MealId INTEGER NOT NULL,
            ProductId INTEGER NOT NULL,
            AmountGrams REAL NOT NULL,
            Calories REAL NOT NULL,
            Protein REAL NOT NULL,
            Fat REAL NOT NULL,
            Carbohydrates REAL NOT NULL,
            FOREIGN KEY (MealId) REFERENCES Meals(Id) ON DELETE CASCADE,
            FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
        );

        CREATE INDEX IF NOT EXISTS IX_MealItems_MealId ON MealItems(MealId);
        CREATE INDEX IF NOT EXISTS IX_MealItems_ProductId ON MealItems(ProductId);

        CREATE TABLE IF NOT EXISTS WaterIntakes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DailyLogId INTEGER NOT NULL,
            AmountMl INTEGER NOT NULL,
            Time TEXT NOT NULL,
            FOREIGN KEY (DailyLogId) REFERENCES DailyLogs(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_WaterIntakes_DailyLogId ON WaterIntakes(DailyLogId);

        CREATE TABLE IF NOT EXISTS Recipes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            Servings INTEGER NOT NULL DEFAULT 1,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_Recipes_UserId ON Recipes(UserId);

        CREATE TABLE IF NOT EXISTS RecipeIngredients (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RecipeId INTEGER NOT NULL,
            ProductId INTEGER NOT NULL,
            AmountGrams REAL NOT NULL,
            FOREIGN KEY (RecipeId) REFERENCES Recipes(Id) ON DELETE CASCADE,
            FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
        );

        CREATE INDEX IF NOT EXISTS IX_RecipeIngredients_RecipeId ON RecipeIngredients(RecipeId);
        CREATE INDEX IF NOT EXISTS IX_RecipeIngredients_ProductId ON RecipeIngredients(ProductId);
    ";

    public static async Task InitializeAsync(SqliteConnection connection, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = SchemaSql;
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await SeedDefaultProductsIfEmptyAsync(connection, ct);
    }

    private static async Task SeedDefaultProductsIfEmptyAsync(SqliteConnection connection, CancellationToken ct)
    {
        using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM Products;";
            var result = await countCmd.ExecuteScalarAsync(ct);
            long count = result != null ? Convert.ToInt64(result) : 0L;
            if (count > 0)
            {
                return;
            }
        }

        var defaultProducts = new (string Name, double Cal, double Prot, double Fat, double Carb, string? Barcode)[]
        {
            ("Куриное филе (грудка)", 113.0, 23.6, 1.9, 0.4, "4607001000011"),
            ("Овсяные хлопья Геркулес", 352.0, 12.3, 6.2, 61.8, "4607001000028"),
            ("Яйцо куриное (1 шт C1 ~55г)", 157.0, 12.7, 11.5, 0.7, "4607001000035"),
            ("Творог 5%", 121.0, 17.0, 5.0, 3.0, "4607001000042"),
            ("Рис длиннозерный белый", 344.0, 6.7, 0.7, 78.9, "4607001000059"),
            ("Гречневая крупа ядрица", 313.0, 12.6, 3.3, 62.1, "4607001000066"),
            ("Банан свежий", 95.0, 1.5, 0.2, 21.8, "4607001000073"),
            ("Яблоко сезонное", 47.0, 0.4, 0.4, 9.8, "4607001000080"),
            ("Лосось (семга) филе", 208.0, 20.0, 13.0, 0.0, "4607001000097"),
            ("Молоко 2.5%", 52.0, 2.9, 2.5, 4.7, "4607001000103"),
            ("Масло оливковое Extra Virgin", 898.0, 0.0, 99.8, 0.0, "4607001000110"),
            ("Макароны из твердых сортов", 344.0, 12.0, 1.1, 71.5, "4607001000127"),
            ("Хлеб цельнозерновой", 213.0, 8.5, 1.3, 42.0, "4607001000134"),
            ("Арахис жареный", 567.0, 26.0, 49.0, 16.0, "4607001000141"),
            ("Огурец свежий", 15.0, 0.8, 0.1, 2.8, "4607001000158"),
            ("Помидор свежий", 20.0, 0.6, 0.2, 4.2, "4607001000165")
        };

        using var transaction = connection.BeginTransaction();
        try
        {
            using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT INTO Products (Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g, Barcode)
                VALUES (@Name, @CaloriesPer100g, @ProteinPer100g, @FatPer100g, @CarbohydratesPer100g, @Barcode);
            ";

            var pName = insertCmd.Parameters.Add("@Name", SqliteType.Text);
            var pCal = insertCmd.Parameters.Add("@CaloriesPer100g", SqliteType.Real);
            var pProt = insertCmd.Parameters.Add("@ProteinPer100g", SqliteType.Real);
            var pFat = insertCmd.Parameters.Add("@FatPer100g", SqliteType.Real);
            var pCarb = insertCmd.Parameters.Add("@CarbohydratesPer100g", SqliteType.Real);
            var pBarcode = insertCmd.Parameters.Add("@Barcode", SqliteType.Text);

            foreach (var p in defaultProducts)
            {
                pName.Value = p.Name;
                pCal.Value = p.Cal;
                pProt.Value = p.Prot;
                pFat.Value = p.Fat;
                pCarb.Value = p.Carb;
                pBarcode.Value = (object?)p.Barcode ?? DBNull.Value;
                await insertCmd.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
