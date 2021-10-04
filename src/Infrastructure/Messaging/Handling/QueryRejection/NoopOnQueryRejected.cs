using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public class NoopOnQueryRejected : IOnQueryRejected
    {
        public Task OnQueryRejected(IQuery query, string[] messages)
            => Task.CompletedTask;
    }
}
