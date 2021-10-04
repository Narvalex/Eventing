using Infrastructure.Messaging;
using System;

namespace Infrastructure.EntityFramework.Tests
{
    public static class MetadataHelper
    {
        public static EventMetadata NewEventMetadata() => new EventMetadata(Guid.NewGuid(),
            "corrId",
            "causId",
            "commitId",
            DateTime.Now,
                "author",
                "name",
                "ip",
                "user_agent");
    }
}
