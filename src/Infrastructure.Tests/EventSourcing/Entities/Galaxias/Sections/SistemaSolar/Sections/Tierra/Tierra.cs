using Infrastructure.EventSourcing;

namespace Infrastructure.Tests.EventSourcing
{
    public class Tierra : EventSourcedSection
    {
        public Tierra(Estaciones? estaciones = null, SubEntities<Continente>? continentes = null, SubEntities<TipoDeSerVivo>? tiposDeSeresVivos = null)
        {
            this.Estaciones = estaciones ?? new Estaciones();
            this.Continentes = continentes ?? new SubEntities<Continente>();
            this.TiposDeSeresVivos = tiposDeSeresVivos ?? new SubEntities<TipoDeSerVivo>();
        }

        public Estaciones Estaciones { get; }
        public SubEntities<Continente> Continentes { get; }
        public SubEntities<TipoDeSerVivo> TiposDeSeresVivos { get; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry
                .AddSection(this.Estaciones)
                .AddSubEntities(this.Continentes)
                .AddSubEntities(this.TiposDeSeresVivos)
                .On<NuevoContinenteRegistrado>(x =>
                        this.Continentes.Add(
                            new Continente(x.IdContinente, x.Nombre)))
                .On<NuevoTipoDeSerVivoRegistrado>(x =>
                    this.TiposDeSeresVivos.Add(
                        new TipoDeSerVivo(x.IdTipoDeSerVivo, x.Nombre)))
            ;
    }
}
