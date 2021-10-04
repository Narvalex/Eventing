using Infrastructure.EventSourcing;

namespace Infrastructure.Tests.EventSourcing
{
    public class Pais : SubEntity2Base
    {
        public Pais(int id, string nombre)
            : base(id)
        {
            this.Nombre = nombre;
        }

        public string Nombre { get; private set; }

        protected override void OnRegisteringHandlers(ISubEntity2HandlerRegistry registry) =>
            registry
                .On<NombreDePaisCorregido>(this.ExtraerIds, x => this.Nombre = x.NombreCorrecto)
            ;


        private (int entityId, int subEntityId) ExtraerIds(PaisEvent e) => (e.IdContinente, e.IdPais);
    }
}
