namespace Infrastructure.Tests.EventSourcing
{
    public class NuevoPaisRegistrado : ContinenteEvent
    {
        public NuevoPaisRegistrado(string idGalaxia, int idContinente, int idPais, string nombre)
            : base(idGalaxia, idContinente)
        {
            this.IdPais = idPais;
            this.Nombre = nombre;
        }

        public int IdPais { get; }
        public string Nombre { get; }
    }
}
