using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;

namespace Erp.Domain.Tests.Helpers
{
    public class ModelSerializationTestHelper
    {
        private readonly IJsonSerializer serializer;

        public ModelSerializationTestHelper(IJsonSerializer serializer)
        {
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));
        }

        public bool SerializationIsValid<T>(T originalPayload)
        {
            var originalText = this.serializer.Serialize(originalPayload);
            var newPlayload = this.serializer.Deserialize(originalText, originalPayload.GetType());
            var newText = this.serializer.Serialize(newPlayload);
            return originalText == newText;
        }

        public bool SerializationIsValid<T>(T originalPayload, Func<T, T> onDeserialized)
        {
            var originalText = this.serializer.Serialize(originalPayload);
            var newPlayload = onDeserialized((T)this.serializer.Deserialize(originalText, originalPayload.GetType()));
            var newText = this.serializer.Serialize(newPlayload);
            return originalText == newText;
        }
    }
}
