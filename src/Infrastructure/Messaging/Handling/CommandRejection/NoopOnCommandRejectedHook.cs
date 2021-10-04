using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class NoopOnCommandRejectedHook : IOnCommandRejected
    {
        public Task OnCommandRejected(ICommand command, string[] messages)
            => Task.CompletedTask;
    }
}
