using Infrastructure.Messaging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing
{
    public static class EventSourcedReaderExtensions
    {
        public static async Task<T> GetByIdAsync<T>(this IEventSourcedReader reader, string streamId) where T : class, IEventSourced
        {
            var eventSourced = await reader.GetByStreamNameAsync<T>(EventStream.GetStreamName<T>(streamId));
            if (eventSourced is null)
                throw new NullReferenceException($"The event sourced of type {typeof(T).Name} with id {streamId} is null");
            return eventSourced;
        }

        public static async Task<T> GetByIdAsync<T>(this IEventSourcedReader reader, string streamId, IEvent @event) where T : class, IEventSourced
        {
            var eventSourced = await reader.GetByStreamNameAsync<T>(EventStream.GetStreamName<T>(streamId), @event.GetEventMetadata().EventNumber);
            if (eventSourced is null)
                throw new NullReferenceException($"The event sourced of type {typeof(T).Name} with id {streamId} is null");
            return eventSourced;
        }

        public static async Task<T?> TryGetByIdAsync<T>(this IEventSourcedReader reader, string streamId) where T : class, IEventSourced
        {
            return await reader.GetByStreamNameAsync<T>(EventStream.GetStreamName<T>(streamId));
        }

        public static async Task<IEventSourced?> TryGetByIdAsync(this IEventSourcedReader reader, Type type, string streamId)
        {
            return await reader.GetByStreamNameAsync(type, EventStream.GetStreamName(type, streamId));
        }

        public static async Task<T?> TryGetByIdAsync<T>(this IEventSourcedReader reader, string streamId, IEvent @event) where T : class, IEventSourced
        {
            return await reader.GetByStreamNameAsync<T>(EventStream.GetStreamName<T>(streamId), @event.GetEventMetadata().EventNumber);
        }

        public static async Task<bool> ExistsAsync<T>(this IEventSourcedReader repository, string streamId) where T : IEventSourced
        {
            return await repository.ExistsAsync(typeof(T), EventStream.GetStreamName<T>(streamId));
        }
    }
}
