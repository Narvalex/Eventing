using Infrastructure.Messaging;
using System;

namespace Infrastructure.EventSourcing
{
    public interface IHandlerRegistry
    {
        IHandlerRegistry On<T>() where T : IEvent;
        IHandlerRegistry On(Type eventType);
        IHandlerRegistry On(Type eventType, Action<object> handler);
        IHandlerRegistry On<T>(Action<T> handler) where T : IEvent;
        IHandlerRegistry AddSection(EventSourcedSection section);
        IHandlerRegistry AddSubEntities(ISubEntities subEntities);
    }
}
