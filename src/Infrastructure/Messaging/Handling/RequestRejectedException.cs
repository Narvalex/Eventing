using System;

namespace Infrastructure.Messaging.Handling
{
    public class RequestRejectedException : Exception
    {
        public RequestRejectedException(params string[] messages)
        {
            this.Messages = messages;
        }

        public string[] Messages { get; }
    }
}
