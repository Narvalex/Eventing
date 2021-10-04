namespace Infrastructure.Messaging
{
    public interface IEventUpcasterRegistry
    {
        IEventUpcasterRegistry Register(IEventUpcaster eventUpcaster);
    }
}
