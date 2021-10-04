using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.EventSourcing
{
    public abstract class StrSubEntity2Base : IStrSubEntity2HandlerRegistry
    {
        public StrSubEntity2Base(string id)
        {
            this.Handlers = new Dictionary<Type, (Func<object, (string entityId, string subEntityId)> idSelectors, Action<object> handler)>();
            this.IgnoredEvents = new List<Type>();

            // If sub entityes level 2 will be created, this need to be called outside the ctor. 
            // Check Entity class
            this.OnRegisteringHandlers(this);
            this.Id = id;
        }

        internal List<Type> IgnoredEvents { get; }

        internal void InvokeHandler(Type eventType, object @event)
        {
            this.Handlers[eventType].handler(@event);
        }

        internal Dictionary<Type, (Func<object, (string entityId, string subEntityId)> idSelectors, Action<object> handler)> Handlers { get; }

        public string Id { get; }

        protected abstract void OnRegisteringHandlers(IStrSubEntity2HandlerRegistry registry);

        IStrSubEntity2HandlerRegistry IStrSubEntity2HandlerRegistry.On<T>()
        {
            this.IgnoredEvents.Add(typeof(T));
            return this;
        }


        IStrSubEntity2HandlerRegistry IStrSubEntity2HandlerRegistry.On<T>(Func<T, (string subEntityId, string subEntity2Id)> idSelectors, Action<T> handler)
        {
            var eventType = typeof(T);
            this.Handlers.Add(eventType, (x => idSelectors((T)x), x => handler((T)x)));
            return this;
        }

        IStrSubEntity2HandlerRegistry IStrSubEntity2HandlerRegistry.AddSection(StrSubEntity2Section section)
        {
            this.Handlers.AddRange(section.Handlers);
            return this;
        }
    }
}
