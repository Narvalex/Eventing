using System;
using System.Data;

namespace Infrastructure.Utils
{
    /// <summary>
    /// Provides usability overloads for <see cref="SqlDataReader"/>.
    /// </summary>
    /// <remarks>
    /// Based on: http://stackoverflow.com/questions/1772025/sql-data-reader-handling-null-column-values
    /// </remarks>
    public static class DataReaderExtensions
    {
        #region Decimal

        public static decimal GetDecimal(this IDataReader reader, string name)
            => (decimal)reader[name];

        public static decimal? GetDecimalOrNull(this IDataReader reader, int i)
            => reader[reader.GetName(i)] as decimal?;

        public static decimal? GetDecimalOrNull(this IDataReader reader, string name)
            => reader[name] as decimal?;

        #endregion

        #region String

        public static string GetString(this IDataReader reader, string name)
            => reader[name] as string;

        #endregion

        #region Char
        public static char GetChar(this IDataReader reader, string name)
           => (char)reader[name];
        #endregion

        #region Int16 

        public static short GetInt16(this IDataReader reader, string name)
            => (short)reader[name];
        public static short? GetInt16OrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(short?)
                                  : reader.GetInt16(i);
        public static short? GetInt16OrNull(this IDataReader reader, string name)
            => reader.GetInt16OrNull(reader.GetOrdinal(name));

        #endregion

        #region Int32

        public static int GetInt32(this IDataReader reader, string name)
           => (int)reader[name];

        public static int GetInt32OrCastFromInt16(this IDataReader reader, string name)
        {
            var value = reader[name];
            if (value is int)
                return (int)value;
            var int16Value = (short)value;
            return int.Parse(int16Value.ToString());
        }

        public static int? GetInt32OrCastFromInt16OrNull(this IDataReader reader, string name)
        {
            var i = reader.GetOrdinal(name);
            if (reader.IsDBNull(i))
                return null;

            return reader.GetInt32OrCastFromInt16(name);
        }

        public static int? GetInt32OrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(int?)
                                  : reader.GetInt32(i);

        public static int? GetInt32OrNull(this IDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? default(int?) : reader.GetInt32(ordinal);
        }


        public static int? GetInt32OrInt16Null(this IDataReader reader, string name)
            => reader.GetInt32OrCastFromInt16(name);

        #endregion

        #region Int64

        public static long GetInt64(this IDataReader reader, string name)
            => (long)reader[name];

        public static long? GetInt64OrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(long?)
                                  : reader.GetInt64(i);

        public static long? GetInt64OrNull(this IDataReader reader, string name)
            => reader.GetInt64OrNull(reader.GetOrdinal(name));

        #endregion

        #region Float

        public static float GetFloat(this IDataReader reader, string name)
            => (float)reader[name];

        public static float? GetFloatOrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(float?)
                                  : reader.GetFloat(i);

        public static float? GetFloatOrNull(this IDataReader reader, string name)
            => reader.GetFloatOrNull(reader.GetOrdinal(name));

        #endregion

        #region Double

        public static double GetDouble(this IDataReader reader, string name)
            => (double)reader[name];

        public static double? GetDoubleOrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(double?)
                                  : reader.GetDouble(i);

        public static double? GetDoubleOrNull(this IDataReader reader, string name)
            => reader.GetDoubleOrNull(reader.GetOrdinal(name));

        #endregion

        #region Guid
        public static Guid GetGuid(this IDataReader reader, string name)
            => (Guid)reader[name];

        public static Guid? GetGuidOrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(Guid?)
                                  : reader.GetGuid(i);

        public static Guid? GetGuidOrNull(this IDataReader reader, string name)
            => reader.GetGuidOrNull(reader.GetOrdinal(name));

        #endregion

        #region DateTime

        public static DateTime GetDateTime(this IDataReader reader, string name)
            => reader.GetDateTime(reader.GetOrdinal(name));

        public static DateTime? GetDateTimeOrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(DateTime?)
                                  : reader.GetDateTime(i);

        public static DateTime? GetDateTimeOrNull(this IDataReader reader, string name)
            => reader.GetDateTimeOrNull(reader.GetOrdinal(name));

        #endregion

        #region Bool

        public static bool GetBoolean(this IDataReader reader, string name)
            => (bool)reader[name];

        public static bool? GetBooleanOrNull(this IDataReader reader, int i)
            => reader.IsDBNull(i) ? default(bool?)
                                  : reader.GetBoolean(i);

        public static bool? GetBooleanOrNull(this IDataReader reader, string name)
            => reader.GetBooleanOrNull(reader.GetOrdinal(name));

        #endregion
    }
}
