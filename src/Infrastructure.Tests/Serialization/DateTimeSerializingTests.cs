using Infrastructure.Serialization;
using System;
using System.Globalization;
using Xunit;

namespace Infrastructure.Tests.Serialization
{
    public class DateTimeSerializingTests
    {
        protected IJsonSerializer sut = new NewtonsoftJsonSerializer();

        [Fact]
        public void when_serializing_custom_date_in_local_then_can_deserialize_with_the_same_value()
        {
            var dateTime = new DateTime(1975, 10, 19, 0, 0, 0, 0, DateTimeKind.Local);
            var serialized = this.sut.Serialize(dateTime);
            var deserialized = this.sut.Deserialize<DateTime>(serialized);

            Assert.Equal(dateTime.ToUniversalTime(), deserialized.ToUniversalTime());
            // this is really hard to understand. Try removing the to universal time.

            // See more here: https://stackoverflow.com/questions/15003335/javascriptserializer-is-subtracting-one-day-from-date?noredirect=1&lq=1
            // And: https://stackoverflow.com/questions/16413790/daylight-saving-time-issue-of-datetime-from-javascriptserializer-serialize-to-to
            // Or here: https://github.com/JamesNK/Newtonsoft.Json/issues/865
        }

        [Fact]
        public void when_serializing_custom_date_to_local_then_can_deserialize_with_the_same_value()
        {
            var dateTime = new DateTime(1975, 10, 19).ToLocalTime();
            var serialized = this.sut.Serialize(dateTime);
            var deserialized = this.sut.Deserialize<DateTime>(serialized);

            Assert.Equal(dateTime.ToUniversalTime(), deserialized.ToUniversalTime());
            // Same as above
        }

        [Fact]
        public void when_serializing_custom_date_in_utc_then_can_deserialize_with_the_same_value()
        {
            var dateTime = new DateTime(1975, 10, 19, 0, 0, 0, 0, DateTimeKind.Local).ToUniversalTime();
            var serialized = this.sut.Serialize(dateTime);
            var deserialized = this.sut.Deserialize<DateTime>(serialized);

            Assert.Equal(dateTime.ToUniversalTime(), deserialized.ToUniversalTime());
        }

        [Fact]
        public void when_serializing_date_in_utc_then_deserialize_with_the_same_value()
        {
            var dateTime = DateTime.Now.ToUniversalTime();
            var serialized = this.sut.Serialize(dateTime);
            var deserialized = this.sut.Deserialize<DateTime>(serialized);

            Assert.Equal(dateTime.ToUniversalTime(), deserialized.ToUniversalTime());
        }
    }
}
