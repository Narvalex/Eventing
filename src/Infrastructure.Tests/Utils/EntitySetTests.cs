using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;
using Xunit;
using System.Linq;

namespace Infrastructure.Tests.Utils
{
    public class EntitySetTests
    {
        protected NewtonsoftJsonSerializer serializer = new NewtonsoftJsonSerializer();

        [Fact]
        public void can_serialize_a_set_of_dates()
        {
            var entitySet = new EntitySet<DateTime>();

            entitySet.Add(new DateTime(2000, 1, 1));
            entitySet.Add(new DateTime(2000, 1, 2));
            entitySet.Add(new DateTime(2000, 1, 3));

            var serialized = this.serializer.Serialize(entitySet);

            var deserializedExplicitly = this.serializer.Deserialize<EntitySet<DateTime>>(serialized);

            Assert.True(entitySet.SequenceEqual(deserializedExplicitly));
        }
    }
}
