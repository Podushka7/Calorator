using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Calorator.Database.Helpers;

/// <summary>
/// Методы расширения для безопасного чтения данных из SqliteDataReader.
/// </summary>
public static class SqliteDataReaderExtensions
{
    public static string GetStringSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    public static string? GetNullableStringSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static int GetInt32Safe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return 0;
        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    public static int? GetNullableInt32Safe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return null;
        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    public static long GetInt64Safe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return 0L;
        return Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    public static double GetDoubleSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return 0.0;
        return Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    public static double? GetNullableDoubleSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return null;
        return Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    public static bool GetBooleanSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return false;
        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) != 0;
    }

    public static DateTime GetDateTimeSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return DateTime.MinValue;

        var val = reader.GetValue(ordinal);
        if (val is DateTime dt) return dt;

        string str = reader.GetString(ordinal);
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return DateTime.TryParse(str, out var fallback) ? fallback : DateTime.MinValue;
    }

    public static TimeSpan GetTimeSpanSafe(this SqliteDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return TimeSpan.Zero;

        string str = reader.GetString(ordinal);
        return TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out var ts) ? ts : TimeSpan.Zero;
    }

    public static TEnum GetEnumSafe<TEnum>(this SqliteDataReader reader, string columnName) where TEnum : struct, Enum
    {
        int intVal = reader.GetInt32Safe(columnName);
        return (TEnum)Enum.ToObject(typeof(TEnum), intVal);
    }
}
