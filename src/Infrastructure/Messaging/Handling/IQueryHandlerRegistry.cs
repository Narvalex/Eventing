namespace Infrastructure.Messaging.Handling
{
    public interface IQueryHandlerRegistry
    {
        IQueryBus Register(IQueryHandler handler);
    }
}
