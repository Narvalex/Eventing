using Infrastructure.EventSourcing;

namespace Infrastructure.Tests.EventSourcing
{
    // Sections
    public class SistemaSolar : EventSourcedSection
    {
        public SistemaSolar(Tierra? tierra = null, Marte? marte = null)
        {
            this.Tierra = tierra ?? new Tierra();
            this.Marte = marte ?? new Marte();
        }

        public Tierra Tierra { get; }
        public Marte Marte { get; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .AddSection(this.Tierra)
                .AddSection(this.Marte)
            ;
    }
}
