namespace Infrastructure.DateTimeProvider
{
    /// <summary>
    /// The important class <see cref="EventSourcing.EventSourced"/> uses this on preparing events!.
    /// </summary>
    public static class DefaultDateTimeProvider
    {
        private static IDateTimeProvider defaultDateTimeProvider = new LocalDateTimeProvider();
        public static IDateTimeProvider Get() => defaultDateTimeProvider;
    }
}
