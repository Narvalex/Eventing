using Infrastructure.Messaging;
using System;

namespace Infrastructure.EventSourcing
{
    public interface IStrSubEntity2HandlerRegistry
    {
        IStrSubEntity2HandlerRegistry On<T>() where T : IEvent;
        IStrSubEntity2HandlerRegistry On<T>(Func<T, (string subEntityId, string subEntity2Id)> idSelectors, Action<T> handler) where T : IEvent;
        IStrSubEntity2HandlerRegistry AddSection(StrSubEntity2Section section);
    }
}
