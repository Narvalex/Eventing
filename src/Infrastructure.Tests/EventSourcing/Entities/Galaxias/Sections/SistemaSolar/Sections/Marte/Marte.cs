using Infrastructure.EventSourcing;

namespace Infrastructure.Tests.EventSourcing
{
    public class Marte : EventSourcedSection
    {
        public Marte(bool estaRegistrado = false)
        {
            this.EstaRegistrado = estaRegistrado;
        }

        public bool EstaRegistrado { get; private set; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry.On<PlanetaMarteRegistrado>(x => this.EstaRegistrado = true);
    }
}
