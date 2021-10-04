using Infrastructure.IdGeneration;

namespace Infrastructure.Messaging
{
    /// <summary>
    /// A command that is handled in a command handler to update an state of an event sourced object.
    /// When the event sourced object is a <see cref="EventSourcing.ISagaExecutionCoordinator"/> then 
    /// the subsequent commands after the initiator should override the correlation id.
    /// When the event sourced object is a <see cref="EventSourcing.IMutexSagaExecutionCoordinator"/> then every 
    /// initiator command starts a new correlation id.
    /// </summary>
    public abstract class Command : Message, ICommandInTransit, ICommandInTest
    {
        private static IUniqueIdGenerator idGenerator = new KestrelUniqueIdGenerator();
        private string? transactionId;
        private bool isTx = false;

        /// <summary>
        /// Creates a command.
        /// </summary>
        /// <param name="commandId">The command id that will be used as correlation id aswell. This is useful for testing porpuses.</param>
        public Command(string? commandId = null)
        {
            this.CommandId = commandId is null ? idGenerator.New() : commandId;

            if (this.CorrelationId is null)
                this.CorrelationId = this.CommandId;

            this.CausationId = $"{CausationIdPrefix}-{this.GetType().Name}-{this.CommandId}";
        }

        public string CommandId { get; private set; }
        
        /// <summary>
        /// The causation Id for events that are emmitd because of command handling. 
        /// </summary>
        public string CausationId { get; private set; }

        /// <summary>
        /// This is overridable to continue a saga from a command.
        /// </summary>
        public virtual string CorrelationId { get; protected set; }

        public const string CausationIdPrefix = "$cmd";

        void ICommandInTransit.SetMetadata(IMessageMetadata metadata) => this.SetMetadata(metadata);

        void ICommandInTransit.SetCorrelationId(string correlationId) => this.CorrelationId = correlationId;

        void ICommandInTest.SetCommandId(string commandId) => this.CommandId = commandId;

        void ICommandInTest.SetCausationId(string causationId) => this.CausationId = causationId;

        public ICommandInTransit SetTransactionId(string transactionId)
        {
            this.transactionId = transactionId;
            this.isTx = true;
            return this;
        }

        public bool TryGetTransactionId(out string? transactionId)
        {
            transactionId = this.transactionId;
            return this.isTx;
        }
    }
}
