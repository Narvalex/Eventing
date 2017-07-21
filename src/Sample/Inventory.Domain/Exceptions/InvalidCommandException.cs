using System;

namespace Inventory.Domain
{
    public class InvalidCommandException : ArgumentException
    {
        public InvalidCommandException(string message, string argument)
            : base(message, argument)
        {

        }
    }
}
