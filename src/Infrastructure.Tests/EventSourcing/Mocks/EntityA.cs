using Infrastructure.EventSourcing;
using System.Collections.Generic;

namespace Infrastructure.Tests.EventSourcing
{
    public class EntityA : EventSourced
    {
        public EntityA(EventSourcedMetadata metadata, string name)
            : base(metadata)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        protected override void OnOutputState()
        {
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry)
        {
        }
    }
}
