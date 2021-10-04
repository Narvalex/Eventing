using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Utils
{
    public static class Ensure
    {
        public static void NotNull<T>(T? argument, string argumentName) where T : class
        {
            if (argument == null) throw new ArgumentNullException(argumentName);
        }

        public static void NotEmpty(string? text, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException($"The text of '{argumentName}' should not be null or white space.");
        }

        public static void NotEmpty<T>(IEnumerable<T> enumerable, string argumentName)
        {
            if (enumerable is null || enumerable.Count() < 1)
                throw new ArgumentException($"'{argumentName}' should not be empty");
        }

        public static void Positive(int number, string argumentName)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
        }

        public static void Positive(long number, string argumentName)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
        }

        public static void Positive(double number, string argumentName)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
        }

        public static void Positive(decimal number, string argumentName)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should be positive.");
        }

        public static void NotNegative(int number, string argumentName)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should not be negative.");
        }

        public static void NotNegative(double number, string argumentName)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should not be negative.");
        }

        public static void NotNegative(decimal number, string argumentName)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} should not be negative.");
        }

        public static void IsAphanumeric(string text, string argumentName)
        {
            if (!text.IsAlphanumeric())
                throw new ArgumentException("The text should be aphanumeric");
        }

        public static void HasNoWhiteSpace(string text, string argumentName)
        {
            if (text.HasWhiteSpace())
                throw new ArgumentException("The text contains white space");
        }

        public static void NotDefault<T>(T argument, string argumentName) where T : class
        {
            if (argument == default(T))
                throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} has default value");
        }

        public static void NotDefault(Guid argument, string argumentName)
        {
            if (argument == default)
                throw new ArgumentOutOfRangeException(argumentName);
        }

        public static void NotDefault(DateTime argument, string argumentName)
        {
            if (argument == default)
                throw new ArgumentOutOfRangeException(argumentName);
        }

        public static void NamespaceIsValid(Type type, string validNamespace)
        {
            var typeNamespace = type.Namespace;

            // WhiteList
            if (typeNamespace == "Infrastructure.IdGeneration" // SequentialNumber Entity
                || typeNamespace == "Infrastructure.EventSourcing.Transactions" // Transactions entity events
            ) 
                return;

            if (type.Namespace != validNamespace)
                throw new InvalidOperationException($"The type {type.FullName} namespace is invalid. The valid namespace is {validNamespace}.");
        }
    }
}
