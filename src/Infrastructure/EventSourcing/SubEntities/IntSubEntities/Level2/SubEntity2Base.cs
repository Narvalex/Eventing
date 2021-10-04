using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.EventSourcing
{

    public abstract class SubEntity2Base : ISubEntity2HandlerRegistry
    {
        public SubEntity2Base(int id)
        {
            this.Handlers = new Dictionary<Type, (Func<object, (int entityId, int subEntityId)> idSelectors, Action<object> handler)>();
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

        internal Dictionary<Type, (Func<object, (int entityId, int subEntityId)> idSelectors, Action<object> handler)> Handlers { get; }
        public int Id { get; }

        protected abstract void OnRegisteringHandlers(ISubEntity2HandlerRegistry registry);

        ISubEntity2HandlerRegistry ISubEntity2HandlerRegistry.On<T>()
        {
            this.IgnoredEvents.Add(typeof(T));
            return this;
        }


        ISubEntity2HandlerRegistry ISubEntity2HandlerRegistry.On<T>(Func<T, (int subEntityId, int subEntity2Id)> idSelectors, Action<T> handler)
        {
            var eventType = typeof(T);
            this.Handlers.Add(eventType, (x => idSelectors((T)x), x => handler((T)x)));
            return this;
        }

        ISubEntity2HandlerRegistry ISubEntity2HandlerRegistry.AddSection(SubEntity2Section section)
        {
            this.Handlers.AddRange(section.Handlers);
            return this;
        }
    }
}
