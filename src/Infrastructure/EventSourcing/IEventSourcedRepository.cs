using Infrastructure.EventSourcing.Transactions;
using Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.EventSourcing
{
    public interface IEventSourcedRepository : IEventSourcedReader
    {
        Task CommitAsync(IEventSourced eventSourced);
        Task AppendAsync(Type type, IEnumerable<IEventInTransit> events, IMessageMetadata incomingMetadata, string correlationId, string causationId);
        Task<IOnlineTransaction> NewTransaction(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata);
    }
}
