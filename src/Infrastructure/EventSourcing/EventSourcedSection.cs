using System;

namespace Infrastructure.EventSourcing
{
    public abstract class EventSourcedSection : IHandlerRegistry, IEventSourcedSection
    {
        private IHandlerRegistry root = null!;

        IHandlerRegistry IHandlerRegistry.AddSection(EventSourcedSection section)
        {
            ((IEventSourcedSection)section).SetRoot((IEventSourced)this.root);
            return this;
        }

        protected abstract void OnRegisteringHandlers(IHandlerRegistry registry);

        IHandlerRegistry IHandlerRegistry.On<T>(Action<T> handler) =>
            this.root.On<T>(handler);

        IHandlerRegistry IHandlerRegistry.On<T>() =>
            this.root.On<T>();

        protected virtual void OnOutputState() { }

        void IEventSourcedSection.SetRoot(IEventSourced eventSourced)
        {
            this.root = (IHandlerRegistry)eventSourced;
            this.OnRegisteringHandlers(this.root);
            eventSourced.RegisterOutputSectionStateAction(this.OnOutputState);
        }

        IHandlerRegistry IHandlerRegistry.On(Type eventType) =>
            this.root.On(eventType);

        IHandlerRegistry IHandlerRegistry.On(Type eventType, Action<object> handler) =>
            this.root.On(eventType, handler);

        IHandlerRegistry IHandlerRegistry.AddSubEntities(ISubEntities entities) =>
            this.root.AddSubEntities(entities);
    }
}
