using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Infrastructure.EventLog;
using Infrastructure.EventStorage;
using Infrastructure.EventStore.Messaging;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.EventStore.EventStorage
{
    public class EsEventStore : IEventStore
    {
        private readonly Func<Task<IEventStoreConnection>> connectionFactory;
        private readonly IJsonSerializer serializer;
        private readonly int writePageSize;
        private readonly IEventDeserializationAndVersionManager versionManager;
        private readonly IWaitForEventLogToBeConsistent eventLogAwaiter;
        private readonly IEventLogReader eventLogReader;


        public EsEventStore(
         Func<Task<IEventStoreConnection>> connectionFactory,
            IJsonSerializer serializer,
            IEventDeserializationAndVersionManager versionManager,
            IWaitForEventLogToBeConsistent eventLogAwaiter,
            IEventLogReader eventLogReader,
            int writePageSize = 500)
        {
            this.connectionFactory = Ensured.NotNull(connectionFactory, nameof(connectionFactory));
            this.serializer = Ensured.NotNull(serializer, nameof(serializer));
            this.writePageSize = Ensured.Positive(writePageSize, nameof(writePageSize));
            this.versionManager = Ensured.NotNull(versionManager, nameof(versionManager));
            this.eventLogAwaiter = eventLogAwaiter.EnsuredNotNull(nameof(eventLogAwaiter));
            this.eventLogReader = eventLogReader.EnsuredNotNull(nameof(eventLogReader));
        }

        public IEventDeserializationAndVersionManager EventVersionManager => this.versionManager;

        public async Task AppendToStreamAsync(string streamName, IEnumerable<IEvent> events)
            => await AppendToStreamAsync(streamName, ExpectedVersion.Any, events);

        public async Task AppendToStreamAsync(string streamName, long expectedVersion, IEnumerable<IEvent> events)
        {
            var count = events.Count();
            if (count < 1)
                return;

            var connection = await this.connectionFactory.Invoke();
            try
            {
                EventData[] eventDataArray;
                if (count <= this.writePageSize)
                {
                    eventDataArray = events.Select(this.MapToEventData).ToArray();
                    await connection.AppendToStreamAsync(streamName, expectedVersion, eventDataArray);
                }
                else
                {
                    var transaction = await connection.StartTransactionAsync(streamName, expectedVersion);

                    var position = 0;
                    do
                    {
                        var pageEvents = events.Skip(position).Take(this.writePageSize).Select(this.MapToEventData).ToArray();
                        await transaction.WriteAsync(pageEvents);
                        position += this.writePageSize;

                    } while (position < count);

                    await transaction.CommitAsync();
                }
            }
            catch (WrongExpectedVersionException ex)
            {
                throw new OptimisticConcurrencyException("An optimistic concurrenty exception ocurred. See inner exceptions.", ex);
            }
        }

        public async Task<bool> CheckStreamExistenceAsync(string streamName)
        {
            var connection = await this.connectionFactory();
            var readResult = await connection.ReadEventAsync(streamName, StreamPosition.Start, false);
            return readResult.Status == EventReadStatus.Success;
        }

        public async Task<string> ReadLastStreamFromCategory(string category, int offset = 0)
        {
            var connection = await this.connectionFactory();
            await this.WaitForLogToBeConsistentWithLastEventCommited(connection);
            return await this.eventLogReader.ReadLastStreamFromCategory(category, offset);
        }

        public async Task<EventStreamSlice> ReadStreamForwardAsync(string streamName, long from, int count)
        {
            var connection = await this.connectionFactory();
            var slice = await connection.ReadStreamEventsForwardAsync(streamName, from, count, false);
            SliceFetchStatus status;
            switch (slice.Status)
            {
                case SliceReadStatus.Success:
                    status = SliceFetchStatus.Success;
                    break;
                case SliceReadStatus.StreamNotFound:
                case SliceReadStatus.StreamDeleted:
                default:
                    status = SliceFetchStatus.StreamNotFound;
                    break;
            }
            return new EventStreamSlice(
                status,
                slice.Events
                     .Select(e => e.ToEventForHydration(this.versionManager))
                     .ToArray(),
                slice.NextEventNumber,
                slice.IsEndOfStream);
        }

        public async Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count)
        {
            var connection = await this.connectionFactory();

            await this.WaitForLogToBeConsistentWithLastEventCommited(connection);

            return await this.eventLogReader.GetCategoryStreamsSliceAsync(category, from, count);
        }

        public async Task<CategoryStreamsSlice> ReadStreamsFromCategoryAsync(string category, long from, int count, long maxEventNumber)
        {
            await this.eventLogAwaiter.WaitForEventLogToBeConsistentToEventNumber(maxEventNumber);

            return await this.eventLogReader.GetCategoryStreamsSliceAsync(category, from, count, maxEventNumber);
        }

        private async Task WaitForLogToBeConsistentWithLastEventCommited(IEventStoreConnection connection)
        {
            var allEventsSlice = await connection.ReadAllEventsBackwardAsync(Position.End, 1, false);
            await this.eventLogAwaiter.WaitForEventLogToBeConsistentToCommitPosition(allEventsSlice.Events.First().OriginalPosition!.Value.CommitPosition);
        }

        private EventData MapToEventData(IEvent x)
        {
            var dataInJson = this.serializer.Serialize(x);
            var dataBytes = Encoding.UTF8.GetBytes(dataInJson);

            var metadata = x.GetEventMetadata();
            var metadataInJson = this.serializer.SerializeDictionary(metadata.ToDictionary());
            var metadataBytes = Encoding.UTF8.GetBytes(metadataInJson);

            var eventType = x.GetType().Name.WithFirstCharInLower();

            return new EventData(metadata.EventId, eventType, true, dataBytes, metadataBytes);
        }
    }
}
