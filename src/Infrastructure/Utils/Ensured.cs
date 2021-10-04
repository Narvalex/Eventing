using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Infrastructure.Utils
{
    public static class Ensured
    {
        public static T NotNull<T>(T? argument, string argumentName) where T : class
        {
            Ensure.NotNull(argument, argumentName);
            return argument;
        }

        public static string NotEmpty(string? argument, string argumentName)
        {
            Ensure.NotEmpty(argument, argumentName);
            return argument;
        }

        public static string IsValidEmail(string email, string argumentName)
        {
            if (!email.IsValidEmailAddress())
                throw new ArgumentException("The email is not valid");

            return email;
        }

        public static IEnumerable<T> NotEmpty<T>(IEnumerable<T> enumerable, string argumentName)
        {
            Ensure.NotEmpty(enumerable, argumentName);
            return enumerable;
        }

        public static T[] NotEmpty<T>(T[] enumerable, string argumentName)
        {
            Ensure.NotEmpty(enumerable, argumentName);
            return enumerable;
        }

        public static int Positive(int number, string argumentName)
        {
            Ensure.Positive(number, argumentName);
            return number;
        }

        public static long Positive(long number, string argumentName)
        {
            Ensure.Positive(number, argumentName);
            return number;
        }

        public static int NotNegative(int number, string argumentName)
        {
            Ensure.NotNegative(number, argumentName);
            return number;
        }

        public static decimal NotNegative(decimal number, string argumentName)
        {
            Ensure.NotNegative(number, argumentName);
            return number;
        }

        public static double NotNegative(double number, string argumentName)
        {
            Ensure.NotNegative(number, argumentName);
            return number;
        }

        public static T NotDefault<T>(T argument, string argumentName) where T : class
        {
            Ensure.NotDefault(argument, argumentName);
            return argument;
        }

        public static Guid NotDefault(Guid argument, string argumentName)
        {
            Ensure.NotDefault(argument, argumentName);
            return argument;
        }

        public static DateTime NotDefault(DateTime argument, string argumentName)
        {
            Ensure.NotDefault(argument, argumentName);
            return argument;
        }


        public static IEnumerable<string> AllUniqueConstStrings<T>(params Type[] moreTypes)
        {
            var pendingQueue = new Queue<Type>();
            pendingQueue.Enqueue(typeof(T));

            for (int i = 0; i < moreTypes.Length; i++)
                pendingQueue.Enqueue(moreTypes[i]);

            var stringList = new List<string>();

            while (pendingQueue.TryDequeue(out var currentType))
            {
                var types = currentType.GetNestedTypes();
                foreach (var t in types)
                    pendingQueue.Enqueue(t);


                var strings = currentType.GetFields(BindingFlags.Public | BindingFlags.Static)
                                .Where(f => f.FieldType == typeof(string))
                                .Select(f => (string)f.GetValue(null));

                stringList.AddRange(strings);
            }

            if (stringList.Count() != stringList.Distinct().Count())
                throw new InvalidOperationException($"The type {typeof(T).FullName} does not contain unique constants strings.");

            return stringList;
        }

        public static Type NamespaceIsValid(Type type, string validNamespace)
        {
            Ensure.NamespaceIsValid(type, validNamespace);
            return type;
        }
    }
}
