using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Infrastructure.EventStorage
{
    [Serializable]
    public class OptimisticConcurrencyException : AggregateException
    {
        public OptimisticConcurrencyException() { }
        public OptimisticConcurrencyException(string message) : base(message) { }
        public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
        public OptimisticConcurrencyException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }

        // Constructor needed for serialization 
        // when exception propagates from a remote server to the client.
        protected OptimisticConcurrencyException(SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
