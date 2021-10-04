using Infrastructure.EventSourcing;
using Infrastructure.Logging;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using System.Threading.Tasks;

namespace Infrastructure.Processing
{
    public interface IPostInitializationTask
    {
        Task Run(ICommandBus commandBus, IMessageMetadata metadata, IEventSourcedReader reader, ILogLite log);
    }
}
