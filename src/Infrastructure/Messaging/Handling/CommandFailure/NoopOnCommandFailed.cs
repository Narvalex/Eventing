using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class NoopOnCommandFailed : IOnCommandFailed
    {
        public Task<IHandlingResult> OnCommandFailed(ICommand command, Exception exception) => Task.FromResult<IHandlingResult>(null);
    }
}
