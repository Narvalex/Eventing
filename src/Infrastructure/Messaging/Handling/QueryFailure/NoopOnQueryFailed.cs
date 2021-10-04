using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class NoopOnQueryFailed : IOnQueryFailed
    {
        public Task<IHandlingResult> OnQueryFailed(IQuery query, Exception exception)
            => Task.FromResult<IHandlingResult>(null);
    }
}
