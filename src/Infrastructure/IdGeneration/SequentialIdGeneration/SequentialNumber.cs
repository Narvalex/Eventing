using Infrastructure.EventSourcing;

namespace Infrastructure.IdGeneration
{
    public class SequentialNumber : EventSourced
    {
        public SequentialNumber(EventSourcedMetadata metadata)
            : base(metadata)
        {
        }

        protected override void OnRegisteringHandlers(IHandlerRegistry registry) => registry.On<NewNumberGenerated>();
    }
}
