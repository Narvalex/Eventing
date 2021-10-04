using Infrastructure.EventStorage;
using Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing
{
    public static class EventSourcedExtensions
    {
        public static bool AlreadyUpdatedBecauseOf<T>(this T eventSourced, IEvent e) where T : IEventSourced =>
            eventSourced.Metadata.LastCausationNumber >= e.GetEventMetadata().EventNumber;

        /// <summary>
        /// Emits a new event beacause of a command handling. The correlation id and causation id will be taken from the command.
        /// </summary>
        public static T Update<T>(this T eventSourced, ICommand command, IEventInTransit @event) where T : IEventSourced
        {
            if (((ICommandInTransit)command).TryGetTransactionId(out var txId))
                @event.SetTransactionId(txId!);
            eventSourced.Update(command.CorrelationId, command.CausationId, null, command.GetMessageMetadata(), true, @event);
            return eventSourced;
        }

        /// <summary>
        /// Emits a new event beacause of an event handling. The correlation id and causation id will be taken from the event.
        /// </summary>
        public static T Update<T>(this T eventSourced, IEvent incomingEvent, IEventInTransit @event) where T : IEventSourced
        {
            if (((IEventInTransit)incomingEvent).TryGetTransactionId(out var txId))
                @event.SetTransactionId(txId!);
            var metadata = incomingEvent.GetEventMetadata();
            eventSourced.Update(metadata.CorrelationId, metadata.EventId.ToString(), metadata.EventNumber, metadata, false, @event);
            return eventSourced;
        }

        public static T Update<T>(this T eventSourced, ICommand command, params IEventInTransit[] events) where T : IEventSourced
        {
            var isTx = ((ICommandInTransit)command).TryGetTransactionId(out var txId);

            var messageMetadata = command.GetMessageMetadata();
            for (int i = 0; i < events.Length; i++)
                eventSourced.Update(command.CorrelationId, command.CausationId, null, messageMetadata, true, isTx ? events[i].SetTransactionId(txId!) : events[i]);

            return eventSourced;
        }

        public static T Update<T>(this T eventSourced, ICommand command, IEnumerable<IEventInTransit> events) where T : IEventSourced
        {
            var isTx = ((ICommandInTransit)command).TryGetTransactionId(out var txId);

            var messageMetadata = command.GetMessageMetadata();

            foreach (var e in events)
                eventSourced.Update(command.CorrelationId, command.CausationId, null, messageMetadata, true, isTx ? e.SetTransactionId(txId!) : e);

            return eventSourced;
        }

        public static T Update<T>(this T eventSourced, IEvent incomingEvent, IEventInTransit @event, params IEventInTransit[] events) where T : IEventSourced
        {
            var isTx = ((IEventInTransit)incomingEvent).TryGetTransactionId(out var txId);

            var metadata = incomingEvent.GetEventMetadata();

            var causationId = metadata.EventId.ToString();
            var causationNumber = metadata.EventNumber;

            // we ask the first event separate form the optional array of events on pourpuse.
            eventSourced.Update(metadata.CorrelationId, causationId, causationNumber, metadata, false, isTx ? @event.SetTransactionId(txId!) : @event);

            for (int i = 0; i < events.Length; i++)
                eventSourced.Update(metadata.CorrelationId, causationId, causationNumber, metadata, false, isTx ? events[i].SetTransactionId(txId!) : events[i]);

            return eventSourced;
        }

        public static T Update<T>(this T eventSourced, IEvent incomingEvent, IEnumerable<IEventInTransit> events) where T : IEventSourced
        {
            var isTx = ((IEventInTransit)incomingEvent).TryGetTransactionId(out var txId);

            var metadata = incomingEvent.GetEventMetadata();

            var causationId = metadata.EventId.ToString();
            var causationNumber = metadata.EventNumber;

            foreach (var e in events)
                eventSourced.Update(metadata.CorrelationId, causationId, causationNumber, metadata, false, isTx ? e.SetTransactionId(txId!) : e);

            return eventSourced;
        }

        public static T Update<T>(this T eventSourced, UpdateEventSourcedParams prepareEventParams, IEventInTransit @event) where T : IEventSourced
        {
            eventSourced.Update(
                 prepareEventParams.CorrelationId,
                 prepareEventParams.CausationId,
                 prepareEventParams.CausationNumber,
                 prepareEventParams.Metadata,
                 prepareEventParams.IsCommandMetadata,
                 @event);

            return eventSourced;
        }

        public static T Update<T>(this T eventSourced, UpdateEventSourcedParams prepareEventParams, IEnumerable<IEventInTransit> events) where T : IEventSourced
        {
            foreach (var e in events)
                eventSourced.Update(prepareEventParams, e);

            return eventSourced;
        }

        public static async Task<bool> TryRehydrate(this IEventSourced eventSourced, string streamName, IEventStore store, long sliceStart, int readPageSize)
        {
            EventStreamSlice currentSlice;
            do
            {
                currentSlice = await store.ReadStreamForwardAsync(streamName, sliceStart, readPageSize);

                switch (currentSlice.Status)
                {
                    case SliceFetchStatus.Success:
                        sliceStart = currentSlice.NextEventNumber;
                        foreach (var e in currentSlice.Events)
                            eventSourced.Apply(e);
                        break;

                    case SliceFetchStatus.StreamNotFound:
                    default:
                        return false;
                }

            } while (!currentSlice.IsEndOfStream);
            eventSourced.ApplyOutputState();

            return true;
        }

        public static async Task<bool> TryRehydrate(this IEventSourced eventSourced, string streamName, DateTime maxDateTime, IEventStore store, long sliceStart, int readPageSize)
        {
            EventStreamSlice currentSlice;
            do
            {
                currentSlice = await store.ReadStreamForwardAsync(streamName, sliceStart, readPageSize);

                switch (currentSlice.Status)
                {
                    case SliceFetchStatus.Success:
                        sliceStart = currentSlice.NextEventNumber;
                        foreach (var e in currentSlice.Events)
                        {
                            var metadata = e.GetEventMetadata();
                            if (metadata.Timestamp > maxDateTime)
                            {
                                goto ReturnResult;
                            }
                            else
                                eventSourced.Apply(e);
                        }
                        break;

                    case SliceFetchStatus.StreamNotFound:
                    default:
                        return false;
                }

            } while (!currentSlice.IsEndOfStream);

        ReturnResult:
            if (eventSourced.Metadata.Version == EventStream.NoEventsNumber)
                return false; // The event sourced was created AFTER the max date time
            else
            {
                eventSourced.ApplyOutputState();
                return true;
            }
        }

        public static async Task<bool> TryRehydrate(this IEventSourced eventSourced, string streamName, long maxVersion, IEventStore store, long sliceStart, int readPageSize)
        {
            EventStreamSlice currentSlice;
            do
            {
                currentSlice = await store.ReadStreamForwardAsync(streamName, sliceStart, readPageSize);

                switch (currentSlice.Status)
                {
                    case SliceFetchStatus.Success:
                        sliceStart = currentSlice.NextEventNumber;
                        foreach (var e in currentSlice.Events)
                        {
                            var metadata = e.GetEventMetadata();
                            if (metadata.EventSourcedVersion > maxVersion)
                            {
                                goto ReturnResult;
                            }
                            else
                                eventSourced.Apply(e);
                        }
                        break;

                    case SliceFetchStatus.StreamNotFound:
                    default:
                        return false;
                }

            } while (!currentSlice.IsEndOfStream);

        ReturnResult:
            if (eventSourced.Metadata.Version == EventStream.NoEventsNumber)
                return false; // The event sourced does not exist
            else
            {
                eventSourced.ApplyOutputState();
                return true;
            }
        }
    }
}
