using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IQueryBus
    {
        Task<IHandlingResult> Send<T>(T query, IMessageMetadata metadata) where T : IQueryInTransit;
    }
}
