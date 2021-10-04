using System;

namespace Erp.Domain.Tests.Helpers
{
    public class InvalidModelSerializationException : Exception
    {
        public InvalidModelSerializationException(string message) : base(message)
        {
        }
    }
}
