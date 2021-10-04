using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Tests.EventSourcing
{
    public abstract class ContinenteEvent : GalaxiaEvent
    {
        protected ContinenteEvent(string idGalaxia, int idContinente) : base(idGalaxia)
        {
            this.IdContinente = idContinente;
        }

        public int IdContinente { get; }
    }
}
