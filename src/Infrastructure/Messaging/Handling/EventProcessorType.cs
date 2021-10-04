using Infrastructure.EventSourcing;

namespace Infrastructure.Messaging.Handling
{
    public enum EventProcessorType
    {
        /// <summary>
        /// Listen to events that is tracked by a <see cref="EventSourced"/>. It can be a <see cref="ISagaExecutionCoordinator"/>.
        /// The suffix conventions are "EventHandler", "EvHandler".
        /// </summary>
        EventHandler,

        /// <summary>
        /// Handles <see cref="PersistentCommand"/> that are emmited by a <see cref="ISagaExecutionCoordinator"/>.
        /// The handling is tracked by a <see cref="EventSourced"/>.
        /// The suffix conventions are "PersistentCommandHandler", "PersistentCmdHandler".
        /// </summary>
        PersistentCommandHandler,

        /// <summary>
        /// Listen to events and projects to ReadModel by a <see cref="IReadModelProjection"/>.
        /// The suffix conventions are "ReadModelProjection", "ReadModelProj".
        /// </summary>
        ReadModelProjection,

        /// <summary>
        /// Listen to events and sends an Email. The email sending is tracked by a <see cref="EventSourced"/>.
        /// The suffix convention is "EmailSender".
        /// </summary>
        EmailSender
    }
}