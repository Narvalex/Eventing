using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Infrastructure.EventSourcing
{
    [Serializable]
    public class InvalidStreamNameException : AggregateException
    {
        public InvalidStreamNameException() { }
        public InvalidStreamNameException(string message) : base(message) { }
        public InvalidStreamNameException(string message, Exception innerException) : base(message, innerException) { }
        public InvalidStreamNameException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }

        // Constructor needed for serialization 
        // when exception propagates from a remote server to the client.
        protected InvalidStreamNameException(SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
