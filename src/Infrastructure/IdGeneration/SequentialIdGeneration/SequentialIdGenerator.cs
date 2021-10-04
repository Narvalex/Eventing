using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Utils;
using System.Threading.Tasks;

namespace Infrastructure.IdGeneration
{
    public class SequentialIdGenerator<T> : ISequentialIdGenerator<T>
    {
        private readonly IEventSourcedRepository repository;
        private readonly string streamName;

        public SequentialIdGenerator(IEventSourcedRepository repository)
        {
            this.repository = Ensured.NotNull(repository, nameof(repository));
            this.streamName = EventStream.GetCategory<T>();
        }

        public async Task<string> NewAsync(ICommand command)
        {
            var (number, numerator) = await GetNewNumber(command.CommandId, command.CausationId, null, command.GetMessageMetadata(), true);

            await this.repository.CommitAsync(numerator);
            return number;
        }

        public async Task<string> NewAsync(IEvent @event)
        {
            var metadata = @event.GetEventMetadata();
            var (number, numerator) = await GetNewNumber(metadata.CorrelationId, metadata.EventId.ToString(), metadata.CausationNumber, metadata, false);

            await this.repository.CommitAsync(numerator);
            return number;
        }

        private async Task<(string number, SequentialNumber aggregate)> GetNewNumber(string correlationId, string causationId, long? causationNumber, IMessageMetadata metadata, bool isCommandMetadata)
        {
            var numerator = await this.repository.TryGetByIdAsync<SequentialNumber>(this.streamName);
            if (numerator == null)
                numerator = EventSourcedCreator.New<SequentialNumber>();

            numerator.Update(correlationId, causationId, causationNumber, metadata, isCommandMetadata, new NewNumberGenerated(this.streamName));

            var number = numerator.Metadata.Version + 1;
            return (number.ToString(), numerator);
        }
    }
}
