using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing
{
    public static class EventSourcedRepositoryExtensions
    {
        public static async Task<T> CommitAsync<T>(this IEventSourcedRepository repository, T eventSourced) where T : class, IEventSourced
        {
            await repository.CommitAsync(eventSourced);
            return eventSourced;
        }

        public static async Task AppendAsync<T>(this IEventSourcedRepository repository, IQuery query, params IEventInTransit[] events) where T : class, IEventSourced
        {
            await repository.AppendAsync<T>(events, query.GetMessageMetadata(), query.GetCorrelationId(), query.QueryId);
        }

        public static async Task AppendAsync<T>(this IEventSourcedRepository repository, ICommand command, params IEventInTransit[] events) where T : class, IEventSourced
        {
            await repository.AppendAsync<T>(events, command.GetMessageMetadata(), command.CorrelationId, command.CausationId);
        }

        public static Task AppendAsync<T>(this IEventSourcedRepository repository, IEnumerable<IEventInTransit> events, IMessageMetadata incomingMetadata, string correlationId, string causationId) where T : class, IEventSourced =>
            repository.AppendAsync(typeof(T), events, incomingMetadata, correlationId, causationId);

        public static async Task<IOnlineTransaction> NewTransaction(this IEventSourcedRepository repository, ICommandInTransit command)
        {
            var tx = await repository.NewTransaction(command.CorrelationId, command.CausationId, null, command.GetMessageMetadata(), true);
            command.SetTransactionId(tx.TransactionId);
            return tx;
        }

        public static async Task<IOnlineTransaction> NewTransaction(this IEventSourcedRepository repository, IEventInTransit incomingEvent)
        {
            var metadata = incomingEvent.GetEventMetadata();
            var tx = await repository.NewTransaction(metadata.CorrelationId, metadata.EventId.ToString(), metadata.EventNumber, metadata, false);
            incomingEvent.SetTransactionId(tx.TransactionId);
            return tx;
        }


        /*** This makes no sense. No free idempotency here. More info see MessageHandler comentary */
        //public static async Task AppendAsync<T>(this IEventSourcedRepository repository, IEvent e, params IEventInTransit[] events) where T : class, IEventSourced
        //{
        //    var metadata = e.GetEventMetadata();
        //    await repository.AppendAsync<T>(events, metadata, metadata.CorrelationId, metadata.CausationId);
        //}
    }
}
