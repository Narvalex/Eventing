using Infrastructure.Messaging.Handling;

namespace Infrastructure.Services
{
    public abstract class Service
    {
        protected IHandlingResult Ok()
        {
            return new HandlingResult(true);
        }

        protected IResponse<TPayload> Ok<TPayload>(TPayload dto)
        {
            return new Response<TPayload>(dto, true);
        }

        protected IResponse<TPayload> Reject<TPayload>(params string[] messages)
        {
            return new Response<TPayload>(default(TPayload), false, messages);
        }

        protected IResponse<TPayload> Reject<TPayload>(TPayload? dto, params string[] messages)
        {
            return new Response<TPayload>(dto, false, messages);
        }
    }
}
