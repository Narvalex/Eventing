namespace Infrastructure.Tests.EventSourcing
{
    public class NombreDePaisCorregido : PaisEvent
    {
        public NombreDePaisCorregido(string idGalaxia, int idContinente, int idPais, string nombreCorrecto) 
            : base(idGalaxia, idContinente, idPais)
        {

            this.NombreCorrecto = nombreCorrecto;
        }


        public string NombreCorrecto { get; }
    }
}
