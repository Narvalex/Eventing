using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IQueryHandler
    { }

    public interface IQueryHandler<T> : IQueryHandler
    {
        Task<IHandlingResult> Handle(T query);
    }

    public interface IQueryHandler<TQuery, TResponse> : IQueryHandler
    {
        Task<IResponse<TResponse>> Handle(TQuery query);
    }
}
