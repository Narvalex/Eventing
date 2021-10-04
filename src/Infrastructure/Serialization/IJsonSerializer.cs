using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;

namespace Infrastructure.Serialization
{
    public interface IJsonSerializer
    {
        string Serialize(object value);

        string SerializeToPlainJSON(object value);

        T Deserialize<T>(string value);

        object? Deserialize(string value, Type type);

        object? Deserialize(string value, string fullTypeName, string assembly);

        string SerializeDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary);

        IDictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string value);
    }
}
