using Infrastructure.EventSourcing;
using System.Collections.Generic;

namespace Infrastructure.Tests.EventSourcing
{
    public class Galaxia : EventSourced
    {
        public Galaxia(EventSourcedMetadata metadata,
            SistemaSolar sistemaSolar)
            : base(metadata)
        {
            this.SistemaSolar = sistemaSolar ?? new SistemaSolar();
        }

        public SistemaSolar SistemaSolar { get; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .AddSection(this.SistemaSolar)
                .On<GalaxiaCreada>()
            ;
    }
}
