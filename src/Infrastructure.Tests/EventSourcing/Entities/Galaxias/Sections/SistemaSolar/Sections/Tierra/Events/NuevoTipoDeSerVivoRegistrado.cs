namespace Infrastructure.Tests.EventSourcing
{
    public class NuevoTipoDeSerVivoRegistrado : GalaxiaEvent
    {
        public NuevoTipoDeSerVivoRegistrado(string idGalaxia, int idTipoDeSerVivo, string nombre) 
            : base(idGalaxia)
        {
            this.IdTipoDeSerVivo = idTipoDeSerVivo;
            this.Nombre = nombre;
        }

        public int IdTipoDeSerVivo { get; }
        public string Nombre { get; }
    }
}
