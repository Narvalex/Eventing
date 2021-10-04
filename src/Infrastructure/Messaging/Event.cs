using Infrastructure.EventSourcing;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public abstract class Event : Message, IEvent, IEventInTransit
    {
        private string? transactionId;
        private bool isTx = false;

        // Lo hacemos explícito para que Visual Studio cree el snipped de constructor 
        // público en vez del privado
        public Event()
        { }

        /// <summary>
        /// El identificador del stream al que pertenece el evento. 
        /// Como es posible que se publique un evento sin exponer el id 
        /// del stream al que pertence, este dato obliga a implementar a 
        /// todos los que hereden de <see cref="Event"/>.
        /// </summary>
        /// <remarks>
        /// Esto es especialmente útil al desnormalizar por ejemplo. Es 
        /// mejor buscar el "IdUsuario" que, por ejemplo, buscar el "SourceId" de 
        /// un evento del stream del tipo "Usuarios".
        /// </remarks>
        /// <remarks>
        /// We use the term source id instead of entity id since from an event 
        /// perspective, the event comes from a "source" that could be an entity, 
        /// and aggregate or whatever
        /// </remarks> 
        public abstract string StreamId { get; }

        bool IEventInTransit.InTransactionNow => this.isTx;

        public IEventMetadata GetEventMetadata() => (IEventMetadata)this.metadata;

        void IEventInTransit.SetEventMetadata(IEventMetadata metadata, string eventType)
        {
            this.SetMetadata(metadata);
            ((IEventMetadataInTransit)this.metadata).SetEventType(eventType);
        }

        public static implicit operator long(Event @event) 
            => @event.GetEventMetadata().EventNumber;

        async Task IEventInTransit.ValidateEvent(IEventSourcedReader reader)
        {
            var validation = ((IValidatable)this).ExecuteBasicValidation();
            if (!validation.IsValid)
                throw new InvalidEventException(validation.Messages);

            var registry = new ForeignKeyRegistry();
            this.RegisterForeignKeys(registry);
            await registry.CheckForeingKeysContraints(reader, this);
        }

        protected virtual void RegisterForeignKeys(IForeignKeyRegistry registry) { }

        public IEventInTransit SetTransactionId(string transactionId)
        {
            this.transactionId = transactionId;
            this.isTx = true;
            return this;
        }

        public bool CheckIfEventBelongsToTransaction(string transactionId) =>
            this.transactionId == transactionId;

        public bool TryGetTransactionId(out string? transactionId)
        {
            transactionId = this.transactionId;
            return this.isTx;
        }
    }
}
