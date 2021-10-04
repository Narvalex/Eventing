using Infrastructure.EventSourcing;
using Infrastructure.EventSourcing.Transactions;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// An event handler base class.
    /// </summary>
    public abstract class EventHandler<T> : MessageHandler<T>, IEventHandler where T : class, IEventSourced
    {
        public EventHandler(IEventSourcedRepository repo)
            : base(repo)
        { }

        protected async Task<(bool notFound, string sagaId, T saga)> ResolveSaga(IEvent @event)
        {
            var sagaId = @event.GetEventMetadata().CorrelationId;
            var saga = await this.repo.TryGetByIdAsync<T>(sagaId);
            return (saga == null, sagaId, saga);
        }

        protected Task<IOnlineTransaction> NewTransaction(IEventInTransit @event) => this.repo.NewTransaction(@event);
    }
}
