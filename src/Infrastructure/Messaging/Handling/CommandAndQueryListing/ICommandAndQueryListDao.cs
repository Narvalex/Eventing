using System;
using System.Collections.Generic;

namespace Infrastructure.Messaging.Handling
{
    public interface ICommandAndQueryListDao
    {
        string ParseRequestToken(IMessage request);
        string ParseRequestToken(Type requestType);
        string ParseRequestToken<T>() where T : IMessage;
        ICollection<string> RequestTokens { get; }
        ICollection<RequestGroupDto> RequestsByGroup { get; }
    }
}
