using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Infrastructure.EventStore.Messaging.Handling
{
    [Serializable]
    public class EsSubscriptionDroppedException : AggregateException
    {
        public EsSubscriptionDroppedException() { }
        public EsSubscriptionDroppedException(string message) : base(message) { }
        public EsSubscriptionDroppedException(string message, SubscriptionDropReason reason) 
            : base(message)
        {
            this.Reason = reason;
        }
        public EsSubscriptionDroppedException(string message, Exception innerException) : base(message, innerException) { }
        public EsSubscriptionDroppedException(string message, SubscriptionDropReason reason, Exception innerException) 
            : base(message, innerException)
        {
            this.Reason = reason;
        }
        public EsSubscriptionDroppedException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }

        // Constructor needed for serialization 
        // when exception propagates from a remote server to the client.
        protected EsSubscriptionDroppedException(SerializationInfo info,
            StreamingContext context) : base(info, context) { }

        public SubscriptionDropReason Reason { get; }
    }
}
