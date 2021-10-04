using Infrastructure.Messaging;
using System;

namespace Infrastructure.EventSourcing
{
    public interface IStrSubEntityHandlerRegistry
    {
        IStrSubEntityHandlerRegistry On<T>() where T : IEvent;
        IStrSubEntityHandlerRegistry On<T>(Func<T, string> idSelector, Action<T> handler) where T : IEvent;
        IStrSubEntityHandlerRegistry AddSubEntities2<T>(StrSubEntities2<T> subEntities2) where T : StrSubEntity2Base;
        IStrSubEntityHandlerRegistry AddSection(StrSubEntitySection section);
    }
}
