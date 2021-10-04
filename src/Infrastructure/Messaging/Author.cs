using Infrastructure.EventSourcing;
using System;

namespace Infrastructure.Messaging
{
    public class Author : ValueObject<Author>
    {
        public Author(string id, string name, string clientIpAddress, string UserAgent, DateTime timestamp)
        {
            this.Id = id;
            this.Name = name;
            this.ClientIpAddress = clientIpAddress;
            this.UserAgent = UserAgent;
            this.Timestamp = timestamp;
        }

        public string Id { get; }
        public string Name { get; }
        public string ClientIpAddress { get; }
        public string UserAgent { get; }
        public DateTime Timestamp { get; }
    }
}
