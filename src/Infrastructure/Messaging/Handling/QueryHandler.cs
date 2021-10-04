namespace Infrastructure.Messaging.Handling
{
    public abstract class QueryHandler : IQueryHandler
    {
        protected Response<TPayload> Ok<TPayload>(TPayload dto)
        {
            return new Response<TPayload>(dto, true);
        }

        protected dynamic Reject(params string[] messages)
        {
            throw new RequestRejectedException(messages);
        }
    }
}
