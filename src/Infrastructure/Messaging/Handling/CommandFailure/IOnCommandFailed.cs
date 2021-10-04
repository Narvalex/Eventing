using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IOnCommandFailed
    {
        /// <summary>
        /// When a command fails, can optionaly return a handling result, or just fail without giving a clue to the client.
        /// </summary>
        Task<IHandlingResult> OnCommandFailed(ICommand command, Exception exception);
    }
}
