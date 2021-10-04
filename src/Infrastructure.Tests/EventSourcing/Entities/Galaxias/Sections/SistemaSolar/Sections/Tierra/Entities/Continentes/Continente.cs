using Infrastructure.EventSourcing;

namespace Infrastructure.Tests.EventSourcing
{
    public class Continente : SubEntityBase
    {
        public Continente(int id, string nombre, PaisesDelContinente? paisesDelContinente = null)
            : base(id)
        {
            this.Nombre = nombre;
            this.PaisesDelContinente = paisesDelContinente ?? new PaisesDelContinente(id);
        }

        public string Nombre { get; }
        public PaisesDelContinente PaisesDelContinente { get; }

        protected override void OnRegisteringHandlers(ISubEntityHandlerRegistry registry) =>
            registry.AddSection(this.PaisesDelContinente);
    }
}
