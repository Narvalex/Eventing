using Infrastructure.EventSourcing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Tests.EventSourcing
{
    public class Estaciones : EventSourcedSection
    {
        public Estaciones(List<string>? lista = null)
        {
            this.Lista = lista ?? new List<string>();
        }

        public List<string> Lista { get; }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) =>
            registry.On<NuevaEstacionTerrestreRegistrada>(x => this.Lista.Add(x.Nombre));
    }
}
