using Infrastructure.EventSourcing;
using System;

namespace Infrastructure.Tests.EventSourcing
{
    public class PaisesDelContinente : SubEntitySection
    {
        public PaisesDelContinente(int id, SubEntities2<Pais>? paises = null) 
            : base(id)
        {
            this.Paises = paises ?? new SubEntities2<Pais>();
        }

        public SubEntities2<Pais> Paises { get; }

        protected override void OnRegisteringHandlers(ISubEntityHandlerRegistry registry) =>
            registry
                .AddSubEntities2(this.Paises)
                .On<NuevoPaisRegistrado>(this.ExtraerIds, x => this.Paises.Add(new Pais(x.IdPais, x.Nombre)))
            ;

        private int ExtraerIds<T>(T @event) where T : ContinenteEvent => @event.IdContinente;

    }
}
