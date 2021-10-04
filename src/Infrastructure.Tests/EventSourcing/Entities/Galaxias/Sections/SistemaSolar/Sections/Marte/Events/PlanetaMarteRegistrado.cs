using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Tests.EventSourcing
{
    public class PlanetaMarteRegistrado : GalaxiaEvent
    {
        public PlanetaMarteRegistrado(string idGalaxia) : base(idGalaxia)
        {
        }
    }
}
