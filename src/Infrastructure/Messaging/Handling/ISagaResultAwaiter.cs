using Infrastructure.EventSourcing;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface ISagaResultAwaiter 
    {
        Task<IHandlingResult> Await<T>(string streamId, Func<T?, IHandlingResult?> awaitLogic) where T : class, IEventSourced;
        Task<IHandlingResult> Await<T>(string streamId, Func<T?, Task<IHandlingResult?>> awaitLogic) where T : class, IEventSourced;
    }
}
