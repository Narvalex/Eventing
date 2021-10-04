using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.Cryptography
{
    public class CryptoSerializer : IJsonSerializer
    {
        private readonly IDecryptor decryptor;
        private readonly IJsonSerializer serializer;

        public CryptoSerializer(IDecryptor decryptor, IJsonSerializer serializer)
        {
            Ensure.NotNull(serializer, nameof(serializer));
            Ensure.NotNull(decryptor, nameof(decryptor));

            this.decryptor = decryptor;
            this.serializer = serializer;
        }

        public T Deserialize<T>(string value)
        {
            return this.serializer.Deserialize<T>(this.decryptor.Decrypt(value));
        }

        public object? Deserialize(string value, Type type)
        {
            return this.serializer.Deserialize(this.decryptor.Decrypt(value), type);
        }

        public object? Deserialize(string value, string fullTypeName, string assembly)
        {
            return this.serializer.Deserialize(this.decryptor.Decrypt(value), fullTypeName, assembly);
        }

        public IDictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string value)
        {
            return this.serializer.DeserializeDictionary<TKey, TValue>(this.decryptor.Decrypt(value));
        }

        public string Serialize(object value)
        {
            return this.decryptor.Encrypt(this.serializer.Serialize(value));
        }

        public string SerializeDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            return this.decryptor.Encrypt(this.serializer.SerializeDictionary(dictionary));
        }

        public string SerializeToPlainJSON(object value)
        {
            return this.decryptor.Encrypt(this.serializer.SerializeToPlainJSON(value));
        }
    }
}
