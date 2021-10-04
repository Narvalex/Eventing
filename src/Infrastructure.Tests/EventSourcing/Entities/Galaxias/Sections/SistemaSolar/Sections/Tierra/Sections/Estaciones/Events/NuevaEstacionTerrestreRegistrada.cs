namespace Infrastructure.Tests.EventSourcing
{
    public class NuevaEstacionTerrestreRegistrada : GalaxiaEvent
    {
        public NuevaEstacionTerrestreRegistrada(string idGalaxia, string nombre) : base(idGalaxia)
        {
            this.Nombre = nombre;
        }

        public string Nombre { get; }
    }
}
