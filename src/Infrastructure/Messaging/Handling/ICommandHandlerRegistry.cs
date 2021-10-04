namespace Infrastructure.Messaging.Handling
{
    public interface ICommandHandlerRegistry
    {
        ICommandBus Register(ICommandHandler handler);
    }
}
