using System;

namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// Indicate the client that a parameter is incorrect. This will be sent from the 
    /// command bus/handler or query handler to the client.
    /// </summary>
    public class ParameterException : Exception
    {
        public ParameterException(params string[] messages)
        {
            this.Messages = messages;
        }

        public string[] Messages { get; }
    }
}
