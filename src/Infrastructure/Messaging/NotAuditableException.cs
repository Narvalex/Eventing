using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Infrastructure.Messaging
{
    [Serializable]
    public class NotAuditableException : AggregateException
    {
        public NotAuditableException() { }
        public NotAuditableException(string message) : base(message) { }
        public NotAuditableException(string message, Exception innerException) : base(message, innerException) { }
        public NotAuditableException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }

        // Constructor needed for serialization 
        // when exception propagates from a remote server to the client.
        protected NotAuditableException(SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
