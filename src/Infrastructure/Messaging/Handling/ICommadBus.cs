using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface ICommandBus
    {
        Task<IHandlingResult> Send(ICommandInTransit command, IMessageMetadata metadata);
    }
}
