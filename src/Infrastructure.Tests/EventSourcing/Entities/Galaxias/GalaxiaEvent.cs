using Infrastructure.Messaging;

namespace Infrastructure.Tests.EventSourcing
{
    // Mundo Events

    public abstract class GalaxiaEvent : Event
    {
        public GalaxiaEvent(string idGalaxia)
        {
            this.IdGalaxia = idGalaxia;
        }

        public override string StreamId => this.IdGalaxia;

        public string IdGalaxia { get; }
    }
}
