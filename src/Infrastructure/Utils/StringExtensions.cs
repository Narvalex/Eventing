using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Utils
{
    public static class StringExtensions
    {
        public static string? ToLowerJavascriptString(this string? text)
        {
            if (text is null) return text;
            var formated = text.Trim().ToLower();
            if (formated == "undefined" || formated == "null" || formated.IsEmpty())
                return null;

            return formated;
        }

        public static string WithFirstCharInLower(this string text)
        {
            return char.ToLower(text[0]) + text.Substring(1);
        }

        public static string WithFirstCharInUpper(this string text)
        {
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        public static bool HasWhiteSpace(this string text)
        {
            return text.Any(c => c == ' ');
        }

        public static bool HasNoWhiteSpace(this string text) => !HasWhiteSpace(text);

        public static bool IsAlphanumeric(this string text)
            => text.NotEmpty() && !new Regex(@"[^\w]").IsMatch(text);

        public static bool IsNumeric(this string text)
            => text.NotEmpty() && text.All(x => 
            x == '0' ||
            x == '1' ||
            x == '2' ||
            x == '3' ||
            x == '4' ||
            x == '5' ||
            x == '6' ||
            x == '7' ||
            x == '8' ||
            x == '9');

        /// <summary>
        /// Indicates whether a specified string is null, empty, or consists only of white-space
        /// characters.
        /// </summary>
        public static bool IsEmpty(this string? text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// Indicates whether a specified string is not null, nor empty, and does not consists only of white-space
        /// characters.
        /// </summary>
        public static bool NotEmpty(this string? text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        public static bool IsValidEmailAddress(this string email)
        {
            if (email.IsEmpty())
                return false;

            // based on: https://stackoverflow.com/questions/5342375/regex-email-validation
            return Regex.IsMatch(email, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public static string WithNewLineCharactersRemoved(this string text)
        {
            // Perf test could be performed to see which is faster
            // More impl. here: https://stackoverflow.com/questions/4140723/how-to-remove-new-line-characters-from-a-string

            var builder = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                if (character != '\n' && character != '\r' && character != '\t')
                    builder.Append(character);
            }

            return builder.ToString();
        }

        public static string WithWhiteSpacesReplaced(this string text, string replacement = "")
        {
            // Perf test could be performed to see which is faster
            // More impl. here: https://stackoverflow.com/questions/4140723/how-to-remove-new-line-characters-from-a-string

            var builder = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                if (character != ' ')
                    builder.Append(character);
                else
                    builder.Append(replacement);
            }

            return builder.ToString();
        }

        public static bool IsEqualWithOrdinalIgnoreCaseComparisson(this string x, string y)
        {
            // Create a StringComparer an compare the hashes.
            return StringComparer.OrdinalIgnoreCase.Compare(x, y) == 0;
        }

        public static string WithDiacriticsRemoved(this string text)
        {
            // http://www.levibotelho.com/development/c-remove-diacritics-accents-from-a-string/

            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        public static byte[] ToByteArrayFromHexString(this string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            var hexValue = _hexValue;
            for (int x = 0, i = 0; i < hexString.Length; i += 2, x += 1)
                bytes[x] = (byte)(hexValue[char.ToUpper(hexString[i + 0]) - '0'] << 4
                            | hexValue[char.ToUpper(hexString[i + 1]) - '0']);
            return bytes;
        }

        private static uint[] _hexValue = new uint[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05,
       0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
       0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
    }
}
