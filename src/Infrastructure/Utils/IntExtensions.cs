using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class IntExtensions
    {

        internal static readonly List<Mes> meses = new List<Mes> {
            new Mes { Key = 1, ShortText = "Ene", LongText="Enero" },
            new Mes { Key = 2, ShortText = "Feb", LongText="Febrero" },
            new Mes { Key = 3, ShortText = "Mar" , LongText="Marzo"},
            new Mes { Key = 4, ShortText = "Abr", LongText="Abril" },
            new Mes { Key = 5, ShortText = "May" , LongText="Mayo"},
            new Mes { Key = 6, ShortText = "Jun", LongText="Junio" },
            new Mes { Key = 7, ShortText = "Jul", LongText="Julio" },
            new Mes { Key = 8, ShortText = "Ago" , LongText="Agosto"},
            new Mes { Key = 9, ShortText = "Set", LongText="Septiembre" },
            new Mes { Key = 10, ShortText = "Oct", LongText="Octubre" },
            new Mes { Key = 11, ShortText = "Nov", LongText="Noviembre" },
            new Mes { Key = 12, ShortText = "Dic" , LongText="Diciembre"},
        };
        public static string ToSpanishShortMonth(this int mes)
        {
            var mesAux = meses.FirstOrDefault(x => x.Key == mes);
            if (mesAux is not null)
                return mesAux.ShortText;
            throw new System.InvalidOperationException("It is only allowed from 1 to 12, as they represent the months");
        }

        public static string ToSpanishLongMonth(this int mes)
        {
            var mesAux = meses.FirstOrDefault(x => x.Key == mes);
            if (mesAux is not null)
                return mesAux.LongText;
            throw new System.InvalidOperationException("It is only allowed from 1 to 12, as they represent the months");
        }

        internal class Mes
        {
            public int Key { get; set; }
            public string ShortText { get; set; }
            public string LongText { get; set; }
        }
    }
}
