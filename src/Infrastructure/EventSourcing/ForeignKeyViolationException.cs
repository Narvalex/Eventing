using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Infrastructure.EventSourcing
{
    [Serializable]
    public class ForeignKeyViolationException : AggregateException
    {
        public ForeignKeyViolationException() { }
        public ForeignKeyViolationException(string message) : base(message) { }
        public ForeignKeyViolationException(string message, Exception innerException) : base(message, innerException) { }
        public ForeignKeyViolationException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions) { }

        // Constructor needed for serialization 
        // when exception propagates from a remote server to the client.
        protected ForeignKeyViolationException(SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}
