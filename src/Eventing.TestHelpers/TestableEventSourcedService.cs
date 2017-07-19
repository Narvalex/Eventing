using Eventing.Core.Domain;
using System;
using System.Collections.Generic;

namespace Eventing.TestHelpers
{
    public class TestableEventSourcedService<T>
        : IGivenReady<T>, IWhenReady<T>, IThenReady, IAndThenReady where T : class
    {
        private readonly IEventSourcedRepository repository;

        public TestableEventSourcedService()
        {
            this.repository = new InMemoryEventSourcedRepository();
        }

        public IWhenReady<T> Given(string streamName, params object[] @event)
        {
            throw new NotImplementedException();
        }

        public IThenReady When(Action<T> handling)
        {
            throw new NotImplementedException();
        }

        public IAndThenReady Then(Action<ICollection<object>> assert)
        {
            throw new NotImplementedException();
        }

        public IWhenReady<T> And(string streamName, params object[] @event)
        {
            throw new NotImplementedException();
        }

        public void And<TSnapshot>(Action<TSnapshot> assert) where TSnapshot : ISnapshot
        {
            throw new NotImplementedException();
        }
    }

    public interface IGivenReady<T>
    {
        IWhenReady<T> Given(string streamName, params object[] @event);
    }

    public interface IWhenReady<T>
    {
        IWhenReady<T> And(string streamName, params object[] @event);
        IThenReady When(Action<T> handling);
    }

    public interface IThenReady
    {
        IAndThenReady Then(Action<ICollection<object>> assert);
    }

    public interface IAndThenReady
    {
        void And<TSnapshot>(Action<TSnapshot> assert) where TSnapshot : ISnapshot;
    }
}
