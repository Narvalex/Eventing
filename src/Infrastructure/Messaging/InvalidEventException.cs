using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Messaging
{

    public class InvalidEventException : Exception
    {
        public InvalidEventException(string[] messages)
            : base(messages.First())
        {
            this.Messages = messages;
        }

        public string[] Messages { get; }
    }
}
