using System;
using Xunit;

namespace Infrastructure.Tests.Playground
{
    public class DateTimeTests
    {
        [Fact]
        public void parse_hour_and_minutes()
        {
            var timeSpan = new TimeSpan(18, 15, 0);
            var dateTime = new DateTime(2020, 1, 1).Date;
            dateTime = dateTime.Add(timeSpan);

            Assert.Equal(1, dateTime.Day);
            Assert.Equal(1, dateTime.Month);
            Assert.Equal(2020, dateTime.Year);
            Assert.Equal(18, dateTime.Hour);
            Assert.Equal(15, dateTime.Minute);
        }

        [Fact]
        public void null_and_datetime_tests()
        {
            var dateTime = new DateTime(2020, 1, 1).Date;
            DateTime? nullTime = null;

            //A null date and any date are not equal or greater or less, they are simply not equal.
            Assert.False(dateTime > nullTime);
            Assert.False(dateTime < nullTime);
            Assert.False(dateTime == nullTime);
            Assert.True(dateTime != nullTime);
        }

        [Fact]
        public void the_default_value_of_DateTime_nulable_is_null()
        {
            var value = default(DateTime?);

            Assert.Null(value);
        }
    }
}
