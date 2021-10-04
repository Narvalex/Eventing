using Infrastructure.Messaging;
using System;

namespace Infrastructure.EventSourcing
{
    public interface ISubEntityHandlerRegistry 
    {
        ISubEntityHandlerRegistry On<T>() where T : IEvent;
        ISubEntityHandlerRegistry On<T>(Func<T, int> idSelector, Action<T> handler) where T : IEvent;
        ISubEntityHandlerRegistry AddSubEntities2<T>(SubEntities2<T> subEntities2) where T : SubEntity2Base;
        ISubEntityHandlerRegistry AddSection(SubEntitySection section);
    }
}
