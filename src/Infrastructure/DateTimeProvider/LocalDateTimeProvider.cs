using System;

namespace Infrastructure.DateTimeProvider
{
    public class LocalDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
