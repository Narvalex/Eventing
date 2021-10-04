using Infrastructure.EventSourcing;

namespace Infrastructure.Tests.EventSourcing
{
    // TipoDeSerVivo Vivos Entities
    public class TipoDeSerVivo : SubEntityBase
    {
        public TipoDeSerVivo(int id, string nombre)
            : base(id)
        {
            this.Nombre = nombre;
        }

        public string Nombre { get; }

        protected override void OnRegisteringHandlers(ISubEntityHandlerRegistry registry)
        {
           
        }
    }
}
