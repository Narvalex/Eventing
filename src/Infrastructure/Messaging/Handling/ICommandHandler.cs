using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    /// <summary>
    /// Marker interface that makes it easier to discover command handlers via reflection
    /// </summary>
    public interface ICommandHandler
    { }

    public interface ICommandHandler<T> : ICommandHandler where T : ICommand
    {
        Task<IHandlingResult> Handle(T cmd);
    }

    public interface ICommandHandler<TCommand, TResponse> : ICommandHandler where TCommand : ICommand
    {
        Task<IResponse<TResponse>> Handle(TCommand cmd);
    }
}
