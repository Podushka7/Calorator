using Calorator.Database.Enums;
using Calorator.Database.Helpers;
using Calorator.Database.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Calorator.Database.Tests.Integration;

/// <summary>
/// Интеграционные и модульные тесты низкоуровневого компонента SqliteDatabase, DatabaseInitializer и SqliteDataReaderExtensions.
/// </summary>
public class SqliteDatabaseTests : IDisposable
{
    private readonly string _dbFilePath;
    private readonly SqliteDatabase _db;

    public SqliteDatabaseTests()
    {
        _dbFilePath = Path.Combine(Path.GetTempPath(), $"calorator_lowlevel_{Guid.NewGuid():N}.db");
        _db = new SqliteDatabase($"Data Source={_dbFilePath};Foreign Keys=True;");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbFilePath))
        {
            try { File.Delete(_dbFilePath); } catch { }
        }
    }

    [Fact]
    public void Constructor_DefaultConnectionString_UsesFallbackWhenNullOrEmpty()
    {
        var defaultDb = new SqliteDatabase(null);
        Assert.Equal(SqliteDatabase.DefaultConnectionString, defaultDb.ConnectionString);

        var whitespaceDb = new SqliteDatabase("   ");
        Assert.Equal(SqliteDatabase.DefaultConnectionString, whitespaceDb.ConnectionString);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_IsIdempotent_AndSeedsDefaultProductsOnce()
    {
        await _db.InitializeDatabaseAsync();
        long firstCount = await _db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Products;");
        Assert.True(firstCount >= 16);

        // Calling InitializeDatabaseAsync a second time should not duplicate seeded products
        await _db.InitializeDatabaseAsync();
        long secondCount = await _db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Products;");
        Assert.Equal(firstCount, secondCount);
    }

    [Fact]
    public async Task ExecuteScalar_ExecuteQuery_ExecuteSingle_WithVariousParameterTypes_WorkProperly()
    {
        await _db.ExecuteNonQueryAsync(@"
            CREATE TABLE TestItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT,
                NullableText TEXT NULL,
                IntVal INTEGER,
                NullableInt INTEGER NULL,
                DoubleVal REAL,
                NullableDouble REAL NULL,
                BoolVal INTEGER,
                EnumVal INTEGER,
                Date AT TEXT,
                CreatedAt TEXT,
                TimeVal TEXT
            );
        ");

        var testDate = new DateTime(2026, 10, 25);
        var testCreated = new DateTime(2026, 10, 25, 14, 30, 0);
        var testTime = new TimeSpan(9, 45, 30);

        // 1. Anonymous object parameters
        int inserted = await _db.ExecuteNonQueryAsync(@"
            INSERT INTO TestItems (Title, NullableText, IntVal, NullableInt, DoubleVal, NullableDouble, BoolVal, EnumVal, Date, CreatedAt, TimeVal)
            VALUES (@Title, @NullableText, @IntVal, @NullableInt, @DoubleVal, @NullableDouble, @BoolVal, @EnumVal, @Date, @CreatedAt, @TimeVal);
        ", new
        {
            Title = "Row1",
            NullableText = (string?)null,
            IntVal = 42,
            NullableInt = (int?)null,
            DoubleVal = 12.5,
            NullableDouble = (double?)null,
            BoolVal = 1,
            EnumVal = MealType.Dinner,
            Date = testDate,
            CreatedAt = testCreated,
            TimeVal = testTime
        });
        Assert.Equal(1, inserted);

        // 2. Dictionary parameters
        var dictParams = new Dictionary<string, object?>
        {
            ["Title"] = "Row2",
            ["@NullableText"] = "Optional",
            ["IntVal"] = 99,
            ["NullableInt"] = 7,
            ["DoubleVal"] = 33.3,
            ["NullableDouble"] = 1.5,
            ["BoolVal"] = 0,
            ["EnumVal"] = MealType.Snack,
            ["Date"] = testDate,
            ["CreatedAt"] = testCreated,
            ["TimeVal"] = testTime
        };

        await _db.ExecuteNonQueryAsync(@"
            INSERT INTO TestItems (Title, NullableText, IntVal, NullableInt, DoubleVal, NullableDouble, BoolVal, EnumVal, Date, CreatedAt, TimeVal)
            VALUES (@Title, @NullableText, @IntVal, @NullableInt, @DoubleVal, @NullableDouble, @BoolVal, @EnumVal, @Date, @CreatedAt, @TimeVal);
        ", dictParams);

        // 3. IEnumerable<SqliteParameter> parameters
        var sqliteParams = new List<SqliteParameter>
        {
            new("@Id", 1)
        };

        var row1 = await _db.ExecuteSingleAsync("SELECT * FROM TestItems WHERE Id = @Id;", reader => new
        {
            Id = reader.GetInt32Safe("Id"),
            LongId = reader.GetInt64Safe("Id"),
            Title = reader.GetStringSafe("Title"),
            NullableText = reader.GetNullableStringSafe("NullableText"),
            IntVal = reader.GetInt32Safe("IntVal"),
            NullableInt = reader.GetNullableInt32Safe("NullableInt"),
            DoubleVal = reader.GetDoubleSafe("DoubleVal"),
            NullableDouble = reader.GetNullableDoubleSafe("NullableDouble"),
            BoolVal = reader.GetBooleanSafe("BoolVal"),
            EnumVal = reader.GetEnumSafe<MealType>("EnumVal"),
            Date = reader.GetDateTimeSafe("Date"),
            CreatedAt = reader.GetDateTimeSafe("CreatedAt"),
            TimeVal = reader.GetTimeSpanSafe("TimeVal")
        }, sqliteParams);

        Assert.NotNull(row1);
        Assert.Equal(1, row1.Id);
        Assert.Equal(1L, row1.LongId);
        Assert.Equal("Row1", row1.Title);
        Assert.Null(row1.NullableText);
        Assert.Equal(42, row1.IntVal);
        Assert.Null(row1.NullableInt);
        Assert.Equal(12.5, row1.DoubleVal);
        Assert.Null(row1.NullableDouble);
        Assert.True(row1.BoolVal);
        Assert.Equal(MealType.Dinner, row1.EnumVal);
        Assert.Equal(testDate, row1.Date);
        Assert.Equal(testCreated, row1.CreatedAt);
        Assert.Equal(testTime, row1.TimeVal);

        // Check Row2 non-null values
        var row2 = await _db.ExecuteSingleAsync("SELECT * FROM TestItems WHERE Id = 2;", reader => new
        {
            NullableText = reader.GetNullableStringSafe("NullableText"),
            NullableInt = reader.GetNullableInt32Safe("NullableInt"),
            NullableDouble = reader.GetNullableDoubleSafe("NullableDouble"),
            BoolVal = reader.GetBooleanSafe("BoolVal")
        });
        Assert.NotNull(row2);
        Assert.Equal("Optional", row2.NullableText);
        Assert.Equal(7, row2.NullableInt);
        Assert.Equal(1.5, row2.NullableDouble);
        Assert.False(row2.BoolVal);

        // Check scalar conversions (Enum, DateTime, TimeSpan, bool, null)
        var scalarEnum = await _db.ExecuteScalarAsync<MealType>("SELECT EnumVal FROM TestItems WHERE Id = 1;");
        Assert.Equal(MealType.Dinner, scalarEnum);

        var scalarDate = await _db.ExecuteScalarAsync<DateTime>("SELECT Date FROM TestItems WHERE Id = 1;");
        Assert.Equal(testDate, scalarDate);

        var scalarTime = await _db.ExecuteScalarAsync<TimeSpan>("SELECT TimeVal FROM TestItems WHERE Id = 1;");
        Assert.Equal(testTime, scalarTime);

        var scalarBool = await _db.ExecuteScalarAsync<bool>("SELECT BoolVal FROM TestItems WHERE Id = 1;");
        Assert.True(scalarBool);

        var scalarNull = await _db.ExecuteScalarAsync<string?>("SELECT NullableText FROM TestItems WHERE Id = 1;");
        Assert.Null(scalarNull);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsOnSuccess_AndRollsBackOnException()
    {
        await _db.InitializeDatabaseAsync();

        // Successful transaction
        await _db.ExecuteInTransactionAsync(async tx =>
        {
            await _db.ExecuteNonQueryAsync(
                "INSERT INTO Products (Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g) VALUES (@Name, 100, 10, 5, 15);",
                new { Name = "TxProduct1" }, tx);

            var single = await _db.ExecuteSingleAsync(
                "SELECT Name FROM Products WHERE Name = @Name;",
                r => r.GetStringSafe("Name"),
                new { Name = "TxProduct1" }, tx);
            Assert.Equal("TxProduct1", single);

            var list = await _db.ExecuteQueryAsync(
                "SELECT Name FROM Products WHERE Name = @Name;",
                r => r.GetStringSafe("Name"),
                new { Name = "TxProduct1" }, tx);
            Assert.Single(list);
        });

        long countAfterCommit = await _db.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Products WHERE Name = 'TxProduct1';");
        Assert.Equal(1L, countAfterCommit);

        // Failing transaction should rollback
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _db.ExecuteInTransactionAsync(async tx =>
            {
                await _db.ExecuteNonQueryAsync(
                    "INSERT INTO Products (Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g) VALUES ('RollbackProd', 50, 1, 1, 1);",
                    null, tx);
                throw new InvalidOperationException("Simulated failure");
            });
        });

        long countAfterRollback = await _db.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Products WHERE Name = 'RollbackProd';");
        Assert.Equal(0L, countAfterRollback);

        // Generic ExecuteInTransactionAsync<T> rollback on exception
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _db.ExecuteInTransactionAsync<int>(async tx =>
            {
                await _db.ExecuteNonQueryAsync(
                    "INSERT INTO Products (Name, CaloriesPer100g, ProteinPer100g, FatPer100g, CarbohydratesPer100g) VALUES ('RollbackProd2', 50, 1, 1, 1);",
                    null, tx);
                throw new InvalidOperationException("Simulated failure 2");
            });
        });

        long countAfterRollback2 = await _db.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Products WHERE Name = 'RollbackProd2';");
        Assert.Equal(0L, countAfterRollback2);
    }
}
