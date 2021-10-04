using Infrastructure.EventSourcing;
using Infrastructure.Messaging;

namespace Erp.Domain.Tests.Helpers
{
    public static class ModelSerializationTestHelperExtensions
    {
        public static void EnsureSerializationIsValid(this ModelSerializationTestHelper helper, ICommandInTest command)
        {
            var commandId = command.CommandId;
            var correlationId = command.CorrelationId;
            var causationId = command.CausationId;

            if (!helper.SerializationIsValid(command, c =>
            {
                c.SetCorrelationId(correlationId);
                c.SetCommandId(commandId);
                c.SetCausationId(causationId);
                return c;
            }))
                throw new InvalidModelSerializationException($"The command '{command.GetType().Name}' serialization is not valid");
        }


        public static void EnsureSerializationIsValid(this ModelSerializationTestHelper helper, IEventInTransit @event)
        {
            if (!helper.SerializationIsValid(@event))
                throw new InvalidModelSerializationException($"The event '{@event.GetType().Name}' serialization is not valid");
        }

        public static void EnsureSerializationIsValid(this ModelSerializationTestHelper helper, IEventSourced entity)
        {
            if (!helper.SerializationIsValid(entity))
                throw new InvalidModelSerializationException($"The event sourced '{entity.GetType().Name}' serialization is not valid");
        }
    }
}
