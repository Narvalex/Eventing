using System;

namespace Infrastructure.Utils
{
    public static class DateTimeExtensions
    {
        public static string ToShortDateStringPyFormat(this DateTime dateTime, string separator = "-") =>
            $"{dateTime.Day}{separator}{dateTime.Month}{separator}{dateTime.Year}";

        public static string ToShortDateStringPyFormat(this DateTime? dateTime, string separator = "-") =>
         dateTime.HasValue ? $"{dateTime.Value.Day}{separator}{dateTime.Value.Month}{separator}{dateTime.Value.Year}" : "";

        public static bool Between(this DateTime dateTime, DateTime min, DateTime max) =>
            dateTime >= min && dateTime <= max;
    }
}
