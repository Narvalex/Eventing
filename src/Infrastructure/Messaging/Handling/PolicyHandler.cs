using Infrastructure.EventSourcing;
using Infrastructure.Utils;

namespace Infrastructure.Messaging.Handling
{
    public abstract class PolicyHandler : RequestHandler
    {
        public PolicyHandler(IEventSourcedReader reader)
            : base(reader)

        {
        }

        protected IHandlingResult Ok()
        {
            return new HandlingResult(true);
        }

        protected IHandlingResult Reject(params string[] messages)
        {
            return new HandlingResult(false, messages);
        }
    }
}
