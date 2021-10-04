using Infrastructure.Messaging;
using System;

namespace Infrastructure.EventSourcing
{
    public interface ISubEntity2HandlerRegistry
    {
        ISubEntity2HandlerRegistry On<T>() where T : IEvent;
        ISubEntity2HandlerRegistry On<T>(Func<T, (int subEntityId, int subEntity2Id)> idSelectors, Action<T> handler) where T : IEvent;
        ISubEntity2HandlerRegistry AddSection(SubEntity2Section section);
    }
}
