namespace Infrastructure.Tests.EventSourcing
{
    public class NuevoContinenteRegistrado : GalaxiaEvent
    {
        public NuevoContinenteRegistrado(string idGalaxia, int idContinente, string nombre) 
            : base(idGalaxia)
        {
            this.IdContinente = idContinente;
            this.Nombre = nombre;
        }

        public int IdContinente { get; }
        public string Nombre { get; }
    }
}
