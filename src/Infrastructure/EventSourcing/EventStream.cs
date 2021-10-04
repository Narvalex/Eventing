using Infrastructure.Messaging;
using Infrastructure.Utils;
using System;

namespace Infrastructure.EventSourcing
{
    public static class EventStream
    {
        public const long NoEventsNumber = -1;

        public static string GetStreamName<T>(string id) => GetStreamName(typeof(T), id);

        public static string GetStreamName(TypeObject type, string id) => GetStreamName(type.ToClrType(), id);

        public static string GetStreamName(Type type, string id)
        {
            var category = GetCategory(type);

            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException($"The category attribute is missing on event sourced type of {type.FullName}");

            return GetStreamName(category, id);
        }

        public static string GetStreamName(string category, string id)
            => $"{category}-{id}";

        public static string GetStreamName(IEvent @event)
        {
            var metadata = @event.GetEventMetadata();
            return $"{metadata.EventSourcedType.WithFirstCharInLower()}-{@event.StreamId}";
        }

        public static string GetId(IEventSourced eventSourced)
            => GetId(eventSourced.Metadata.StreamName);

        public static string GetId(string streamName)
        {
            return streamName.Substring(streamName.IndexOf('-') + 1);
        }

        public static string GetCategory<T>()
        {
            return GetCategory(typeof(T));
        }

        public static string? GetCategory(Type type) => type.Name.WithFirstCharInLower();

        public static string GetCategory(string streamName) => streamName.Split('-')[0];

        public static string GetCategoryProjectionStream<T>()
        {
            return GetCategoryProjectionStream(typeof(T));
        }

        public static string GetCategoryProjectionStream(Type type)
        {
            var category = GetCategory(type);
            return GetCategoryProjectionStream(category);
        }

        public static string GetCategoryProjectionStream(string category)
            => $"$ce-{category}";
    }
}
