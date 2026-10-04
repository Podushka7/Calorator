using Microsoft.Data.Sqlite;

namespace Calorator.Database.Contracts;

/// <summary>
/// Интерфейс низкоуровневого взаимодействия с базой данных SQLite (IDataBase на диаграмме компонентов).
/// Отвечает за управление подключениями, транзакциями, выполнение SQL-запросов и инициализацию схемы.
/// </summary>
public interface IDataBase
{
    /// <summary>
    /// Строка подключения к базе данных SQLite.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Создает новый экземпляр подключения SQLite.
    /// </summary>
    SqliteConnection CreateConnection();

    /// <summary>
    /// Создает и открывает новое подключение SQLite с включенными внешними ключами.
    /// </summary>
    Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Инициализирует схему базы данных (создание таблиц, индексов, триггеров и базовых данных).
    /// </summary>
    Task InitializeDatabaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Выполняет команду без возврата строк (INSERT, UPDATE, DELETE, DDL).
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null, SqliteTransaction? transaction = null, CancellationToken ct = default);

    /// <summary>
    /// Выполняет скалярный запрос и возвращает первое значение первой строки.
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, SqliteTransaction? transaction = null, CancellationToken ct = default);

    /// <summary>
    /// Выполняет запрос на выборку данных и преобразует каждую строку через функцию сопоставления.
    /// </summary>
    Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(string sql, Func<SqliteDataReader, T> map, object? parameters = null, SqliteTransaction? transaction = null, CancellationToken ct = default);

    /// <summary>
    /// Выполняет запрос и возвращает первую запись или null, если ничего не найдено.
    /// </summary>
    Task<T?> ExecuteSingleAsync<T>(string sql, Func<SqliteDataReader, T> map, object? parameters = null, SqliteTransaction? transaction = null, CancellationToken ct = default);

    /// <summary>
    /// Выполняет блок операций внутри транзакции.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<SqliteTransaction, Task> action, CancellationToken ct = default);

    /// <summary>
    /// Выполняет блок операций внутри транзакции с возвратом результата.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<SqliteTransaction, Task<T>> action, CancellationToken ct = default);
}
