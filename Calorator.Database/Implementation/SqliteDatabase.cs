using Calorator.Database.Contracts;
using Microsoft.Data.Sqlite;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Calorator.Database.Implementation;

/// <summary>
/// Реализация низкоуровневого доступа к SQLite (компонент SQLite на диаграмме).
/// Обеспечивает пул подключений, включение Foreign Keys, WAL-режим и потокобезопасные транзакции.
/// </summary>
public class SqliteDatabase : IDataBase
{
    public const string DefaultConnectionString = "Data Source=calorator.db;Foreign Keys=True;";

    public string ConnectionString { get; }

    public SqliteDatabase(string? connectionString = null)
    {
        ConnectionString = string.IsNullOrWhiteSpace(connectionString)
            ? DefaultConnectionString
            : connectionString;
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(ct);

        // Включаем поддержку внешних ключей и оптимизацию производительности SQLite
        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            await pragmaCmd.ExecuteNonQueryAsync(ct);

            // Если база файловая (не in-memory), включаем WAL (Write-Ahead Logging) для быстрой многопоточной работы
            if (!ConnectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
                await pragmaCmd.ExecuteNonQueryAsync(ct);
            }
        }

        return connection;
    }

    public async Task InitializeDatabaseAsync(CancellationToken ct = default)
    {
        using var connection = await OpenConnectionAsync(ct);
        await DatabaseInitializer.InitializeAsync(connection, ct);
    }

    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        object? parameters = null,
        SqliteTransaction? transaction = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (transaction != null)
        {
            using var cmd = transaction.Connection!.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);
            return await cmd.ExecuteNonQueryAsync(ct);
        }

        using var connection = await OpenConnectionAsync(ct);
        using var standaloneCmd = connection.CreateCommand();
        standaloneCmd.CommandText = sql;
        AddParameters(standaloneCmd, parameters);
        return await standaloneCmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        SqliteTransaction? transaction = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (transaction != null)
        {
            using var cmd = transaction.Connection!.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);
            var res = await cmd.ExecuteScalarAsync(ct);
            return ConvertValue<T>(res);
        }

        using var connection = await OpenConnectionAsync(ct);
        using var standaloneCmd = connection.CreateCommand();
        standaloneCmd.CommandText = sql;
        AddParameters(standaloneCmd, parameters);
        var scalarRes = await standaloneCmd.ExecuteScalarAsync(ct);
        return ConvertValue<T>(scalarRes);
    }

    public async Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(
        string sql,
        Func<SqliteDataReader, T> map,
        object? parameters = null,
        SqliteTransaction? transaction = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(map);

        if (transaction != null)
        {
            using var cmd = transaction.Connection!.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            var list = new List<T>();
            while (await reader.ReadAsync(ct))
            {
                list.Add(map(reader));
            }
            return list;
        }

        using var connection = await OpenConnectionAsync(ct);
        using var standaloneCmd = connection.CreateCommand();
        standaloneCmd.CommandText = sql;
        AddParameters(standaloneCmd, parameters);
        using var standaloneReader = await standaloneCmd.ExecuteReaderAsync(ct);

        var standaloneList = new List<T>();
        while (await standaloneReader.ReadAsync(ct))
        {
            standaloneList.Add(map(standaloneReader));
        }
        return standaloneList;
    }

    public async Task<T?> ExecuteSingleAsync<T>(
        string sql,
        Func<SqliteDataReader, T> map,
        object? parameters = null,
        SqliteTransaction? transaction = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(map);

        if (transaction != null)
        {
            using var cmd = transaction.Connection!.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return map(reader);
            }
            return default;
        }

        using var connection = await OpenConnectionAsync(ct);
        using var standaloneCmd = connection.CreateCommand();
        standaloneCmd.CommandText = sql;
        AddParameters(standaloneCmd, parameters);
        using var standaloneReader = await standaloneCmd.ExecuteReaderAsync(ct);
        if (await standaloneReader.ReadAsync(ct))
        {
            return map(standaloneReader);
        }
        return default;
    }

    public async Task ExecuteInTransactionAsync(Func<SqliteTransaction, Task> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var connection = await OpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();
        try
        {
            await action(transaction);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<SqliteTransaction, Task<T>> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var connection = await OpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await action(transaction);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static void AddParameters(SqliteCommand cmd, object? parameters)
    {
        if (parameters == null) return;

        if (parameters is IEnumerable<SqliteParameter> sqliteParameters)
        {
            foreach (var p in sqliteParameters)
            {
                cmd.Parameters.Add(p);
            }
            return;
        }

        if (parameters is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                string rawName = entry.Key.ToString()!.TrimStart('@');
                string paramName = "@" + rawName;
                cmd.Parameters.AddWithValue(paramName, FormatParameterValue(rawName, entry.Value));
            }
            return;
        }

        var properties = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            string rawName = prop.Name;
            string paramName = "@" + rawName;
            object? val = prop.GetValue(parameters);
            cmd.Parameters.AddWithValue(paramName, FormatParameterValue(rawName, val));
        }
    }

    private static object FormatParameterValue(string propertyName, object? val)
    {
        if (val == null)
        {
            return DBNull.Value;
        }

        if (val is Enum enumVal)
        {
            return Convert.ToInt32(enumVal, CultureInfo.InvariantCulture);
        }

        if (val is DateTime dtVal)
        {
            if (propertyName.Equals("Date", StringComparison.OrdinalIgnoreCase))
            {
                return dtVal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            return dtVal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (val is TimeSpan tsVal)
        {
            return tsVal.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        return val;
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value == null || value is DBNull) return default;

        Type targetType = typeof(T);
        Type? underlying = Nullable.GetUnderlyingType(targetType);
        Type nonNullableType = underlying ?? targetType;

        if (nonNullableType.IsEnum)
        {
            return (T)Enum.ToObject(nonNullableType, Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        if (nonNullableType == typeof(DateTime))
        {
            if (value is DateTime dt) return (T)(object)dt;
            string str = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return (T)(object)parsed;
            }
            return DateTime.TryParse(str, out var fallback) ? (T)(object)fallback : default;
        }

        if (nonNullableType == typeof(TimeSpan))
        {
            if (value is TimeSpan ts) return (T)(object)ts;
            string str = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out var parsedTs)
                ? (T)(object)parsedTs
                : default;
        }

        if (nonNullableType == typeof(bool))
        {
            if (value is bool b) return (T)(object)b;
            return (T)(object)(Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0);
        }

        return (T)Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);
    }
}
