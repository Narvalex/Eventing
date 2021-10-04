using Infrastructure.Reflection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.EventSourcing
{
    // Hash: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.hashalgorithm.computehash?view=netcore-3.1
    // Solution: https://stackoverflow.com/questions/28108498/how-can-i-get-a-hash-of-the-body-of-a-method-in-net
    // WARNING: The IL generated change when the file change, even if the class did not changed at all.
    // Disclaimer: After lots of tests it was found that when an EventSourced change at least that eventsourced will have a diff
    // hash, but it is possible that some other event souced will have a different hash aswell.
    public static class EventSourcedEntityHasher
    {
        private static readonly ConcurrentDictionary<Type, string> hashesByType = new ConcurrentDictionary<Type, string>();
        private const string ON_REGISTERING_HANDLERS = "OnRegisteringHandlers";
        private const string ON_OUTPUT_STATE = "OnOutputState";

        public static string GetHash<T>() where T : IEventSourced
        {
            var type = typeof(T);

            return GetHash(type);
        }

        public static string GetHash(Type type)
        {
            if (hashesByType.TryGetValue(type, out var value))
                return value;

            var constructorIl = GetConstructorIL(type);
            var handlingMethodIl = GetMethodIL(type, ON_REGISTERING_HANDLERS);
            var outputMethodIl = GetMethodIL(type, ON_OUTPUT_STATE);

            var allBytes = constructorIl!.Concat(handlingMethodIl!).Concat(outputMethodIl!).ToArray();

            using (var sha256Hash = SHA256.Create())
            {
                value = GetHash(sha256Hash, allBytes);
                hashesByType[type] = value;
                return value;
            }
        }

        public static string GetConstructorIL<T>() where T : IEventSourced =>
            MethodFormatter.FormatMethodBody(
                typeof(T)
                .GetConstructors()
                .First());

        public static string GetHandlersIL<T>() where T : IEventSourced =>
            GetFormattedMethodIL(typeof(T), ON_REGISTERING_HANDLERS);

        public static string GetOnOutputStateIL<T>() where T : IEventSourced =>
            GetFormattedMethodIL(typeof(T), ON_OUTPUT_STATE);

        private static string GetFormattedMethodIL(Type type, string methodName) =>
             MethodFormatter.FormatMethodBody(type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!);

        private static byte[]? GetConstructorIL(Type type) =>
            type
            .GetConstructors()
            .First()
            .GetInstructions()
            .SelectMany(x => Encoding.UTF8.GetBytes(MethodFormatter.FormatInstruction(x)))
            .ToArray();

        private static byte[]? GetMethodIL(Type type, string methodName) =>
            type
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetInstructions()
            .SelectMany(x => Encoding.UTF8.GetBytes(MethodFormatter.FormatInstruction(x)))
            .ToArray();

        private static string GetHash(HashAlgorithm hashAlgorithm, byte[]? byteArray)
        {
            var data = hashAlgorithm.ComputeHash(byteArray!);

            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}
