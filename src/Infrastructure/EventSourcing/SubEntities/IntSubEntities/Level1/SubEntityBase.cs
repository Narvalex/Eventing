using Infrastructure.Utils;
using System;
using System.Collections.Generic;

namespace Infrastructure.EventSourcing
{
    public abstract class SubEntityBase : ISubEntityHandlerRegistry
    {
        private bool handlersResolved = false;
        private Dictionary<Type, (Func<object, int> idSelector, Action<object> handler)> handlers = new Dictionary<Type, (Func<object, int> idSelector, Action<object> handler)>();

        public SubEntityBase(int id)
        {
            this.IgnoredEvents = new List<Type>();
            this.Id = id;
        }

        internal Dictionary<Type, (Func<object, int> idSelector, Action<object> handler)> Handlers
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
        public int Id { get; }
        internal void InvokeHandler(Type eventType, object @event)
        {
            this.Handlers[eventType].handler(@event);
        }

        protected abstract void OnRegisteringHandlers(ISubEntityHandlerRegistry registry);

        ISubEntityHandlerRegistry ISubEntityHandlerRegistry.On<T>()
        {
            this.IgnoredEvents.Add(typeof(T));
            return this;
        }

        ISubEntityHandlerRegistry ISubEntityHandlerRegistry.On<T>(Func<T, int> idSelector, Action<T> handler)
        {
            var eventType = typeof(T);
            this.handlers.Add(eventType, (x => idSelector((T)x), x => handler((T)x)));
            return this;
        }

        ISubEntityHandlerRegistry ISubEntityHandlerRegistry.AddSubEntities2<TSubEntity>(SubEntities2<TSubEntity> subEntities)
        {
            var subEntitySample = ObjectCreator.New<TSubEntity>();

            subEntitySample.Handlers.ForEach(pair =>
                this.handlers.Add(
                    pair.Key,
                    (x => pair.Value.idSelectors(x).entityId, x => subEntities[pair.Value.idSelectors(x).subEntityId].InvokeHandler(pair.Key, x))
            ));

            return this;
        }

        ISubEntityHandlerRegistry ISubEntityHandlerRegistry.AddSection(SubEntitySection section)
        {
            this.handlers.AddRange(section.Handlers);
            return this;
        }
    }
}
