using Infrastructure.DateTimeProvider;
using Infrastructure.Serialization;

namespace Infrastructure.EntityFramework.ReadModel.NoSQL
{
    public static class DocumentEntityDependenciesConfig
    {
        static DocumentEntityDependenciesConfig()
        {
            // Can be configured here;
            Serializer = new NewtonsoftJsonSerializer();
            Timestamp = DefaultDateTimeProvider.Get();
        }

        internal static IJsonSerializer Serializer { get; private set; }
        internal static IDateTimeProvider Timestamp { get; private set; }
    }
}
