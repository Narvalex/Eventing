using Infrastructure.EventSourcing;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public interface IEventInTransit : IEvent
    {
        void SetEventMetadata(IEventMetadata metadata, string eventType);

        Task ValidateEvent(IEventSourcedReader reader);

        IEventInTransit SetTransactionId(string transactionId);
        bool InTransactionNow { get; }
        bool TryGetTransactionId(out string? transactionId);
        bool CheckIfEventBelongsToTransaction(string transactionId);
    }
}
