using Eventing.Core.Domain;
using System;
using System.Threading.Tasks;

namespace Eventing.Core.Persistence
{
    public static class EventSourcedReaderExtensions
    {
        public static async Task<T> GetByIdAsync<T>(this IEventSourcedReader reader, string streamId)
            where T : class, IEventSourced, new()
        {
            return await reader.GetAsync<T>(GetStreamName<T>(streamId));
        }

        public static async Task<T> GetOrFailAsync<T>(this IEventSourcedReader reader, string streamName)
           where T : class, IEventSourced, new()
        {
            var state = await reader.GetAsync<T>(streamName);
            if (state is null) throw new InvalidOperationException($"The stream {streamName} does not exists!");
            return state;
        }

        public static async Task<T> GetOrFailByIdAsync<T>(this IEventSourcedReader reader, string streamId)
           where T : class, IEventSourced, new()
        {
            return await reader.GetOrFailAsync<T>(GetStreamName<T>(streamId));
        }

        public static async Task<bool> Exists<T>(this IEventSourcedReader reader, string streamId)
            where T : class, IEventSourced, new()
        {
            return await reader.Exists(GetStreamName<T>(streamId));
        }

        public static async Task EnsureExistence<T>(this IEventSourcedReader reader, string streamId)
            where T : class, IEventSourced, new()
        {
            await reader.EnsureExistence(GetStreamName<T>(streamId));
        }

        public static async Task EnsureExistence(this IEventSourcedReader reader, string streamName)
        {
            if (await reader.Exists(streamName)) return;
            throw new InvalidOperationException($"The stream {streamName} does not exists!");
        }

        public static async Task<BulkExistenceChecker> EnsureExistenceOf<T>(this IEventSourcedReader reader, string streamId)
            where T : class, IEventSourced, new()
        {
            await reader.EnsureExistence(GetStreamName<T>(streamId));
            return new BulkExistenceChecker(reader);
        }

        private static string GetStreamName<T>(string streamId)
            where T : class, IEventSourced, new()
            => StreamCategoryAttribute.GetFullStreamName<T>(streamId);

        public class BulkExistenceChecker
        {
            private readonly IEventSourcedReader reader;

            public BulkExistenceChecker(IEventSourcedReader reader)
            {
                this.reader = reader;
            }

            public async Task<BulkExistenceChecker> And<T>(string streamId)
                where T : class, IEventSourced, new()
            {
                await this.reader.EnsureExistence<T>(streamId);
                return this;
            }
        }
    }
}
