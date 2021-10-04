using Infrastructure.IdGeneration;

namespace Infrastructure.Messaging
{
    public abstract class Query : Message, IQueryInTransit
    {
        private static IUniqueIdGenerator idGenerator = new KestrelUniqueIdGenerator();

        private readonly string correlationId;

        public Query(string? queryId = null)
        {
            this.correlationId = queryId is null ? idGenerator.New() : queryId;
            this.QueryId = $"$query-{this.correlationId}";
        }
        public string QueryId { get; }

        void IQueryInTransit.SetMetadata(IMessageMetadata metadata) => this.SetMetadata(metadata);

        protected virtual ValidationResult OnExecutingBasicValidation() => this.IsValid();

        ValidationResult IValidatable.ExecuteBasicValidation() => this.OnExecutingBasicValidation();

        public string GetCorrelationId() => this.correlationId;
    }
}
