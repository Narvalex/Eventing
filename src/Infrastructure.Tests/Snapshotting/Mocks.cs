using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using System.Collections.Generic;

namespace Infrastructure.Tests.EventSourcing
{
    public class SnapshotTestEntity : EventSourced
    {
        public SnapshotTestEntity(EventSourcedMetadata metadata, string name, int age) 
            : base(metadata)
        {
            this.Name = name;
            this.Age = age;
        }

        public string Name { get; private set; }
        public int Age { get; private set; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry)
        {
            registry.On<DataSet>(x =>
            {
                this.Name = x.Name;
                this.Age = x.Age;
            });
        }
    }

    public class DataSet : Event
    {

        public DataSet(string id, string name, int age)
        {
            this.Id = id;
            this.Name = name;
            this.Age = age;
        }

        public string Id { get; }
        public string Name { get; }
        public int Age { get; }

        public override string StreamId => this.Id;
    }
}
