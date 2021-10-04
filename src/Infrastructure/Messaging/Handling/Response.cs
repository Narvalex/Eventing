namespace Infrastructure.Messaging.Handling
{
    // This returns a response, as a query result
    public class Response<T> : HandlingResult, IResponse<T>
    {
        public Response(T payload, bool success = true, params string[] messages)
            : base(success, messages)
        {
            this.Payload = payload;
        }

        public T Payload { get; }
    }
}
