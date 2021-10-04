namespace Infrastructure.EventSourcing
{
    public interface ISubEntities
    {
        void OnRegisteringHandlers(IHandlerRegistry eventSourced);
    }
}
