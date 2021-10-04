namespace Infrastructure.Tests.EventSourcing
{
    public abstract class PaisEvent : ContinenteEvent
    {
        public PaisEvent(string idGalaxia, int idContinente, int idPais) 
            : base(idGalaxia, idContinente)
        {
            this.IdPais = idPais;
        }

        public int IdPais { get; }
    }
}
