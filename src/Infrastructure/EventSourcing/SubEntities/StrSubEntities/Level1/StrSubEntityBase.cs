using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.EventSourcing
{
    public abstract class StrSubEntityBase : IStrSubEntityHandlerRegistry
    {
        private bool handlersResolved = false;
        private Dictionary<Type, (Func<object, string> idSelector, Action<object> handler)> handlers = new Dictionary<Type, (Func<object, string> idSelector, Action<object> handler)>();

        private StrSubEntityBase() { }

        public StrSubEntityBase(string id)
        {
            this.IgnoredEvents = new List<Type>();
            this.Id = id;
        }

        internal Dictionary<Type, (Func<object, string> idSelector, Action<object> handler)> Handlers
        {
            get
            {
                if (!this.handlersResolved)
                {
                    this.OnRegisteringHandlers(this);
                    this.handlersResolved = true;
                }

                return this.handlers;
            }
        }


        internal List<Type> IgnoredEvents { get; }
        public string Id { get; }
        internal void InvokeHandler(Type eventType, object @event)
        {
            this.Handlers[eventType].handler(@event);
        }

        protected abstract void OnRegisteringHandlers(IStrSubEntityHandlerRegistry registry);

        IStrSubEntityHandlerRegistry IStrSubEntityHandlerRegistry.On<T>()
        {
            this.IgnoredEvents.Add(typeof(T));
            return this;
        }

        IStrSubEntityHandlerRegistry IStrSubEntityHandlerRegistry.On<T>(Func<T, string> idSelector, Action<T> handler)
        {
            var eventType = typeof(T);
            this.handlers.Add(eventType, (x => idSelector((T)x), x => handler((T)x)));
            return this;
        }

        IStrSubEntityHandlerRegistry IStrSubEntityHandlerRegistry.AddSubEntities2<T>(StrSubEntities2<T> subEntities2)
        {
            var subEntitySample = ObjectCreator.New<T>();

            subEntitySample.Handlers.ForEach(pair =>
                this.handlers.Add(
                    pair.Key,
                    (x => pair.Value.idSelectors(x).entityId, x => subEntities2[pair.Value.idSelectors(x).subEntityId].InvokeHandler(pair.Key, x))
            ));

            return this;
        }

        IStrSubEntityHandlerRegistry IStrSubEntityHandlerRegistry.AddSection(StrSubEntitySection section)
        {
            this.handlers.AddRange(section.Handlers);
            return this;
        }
    }
}
