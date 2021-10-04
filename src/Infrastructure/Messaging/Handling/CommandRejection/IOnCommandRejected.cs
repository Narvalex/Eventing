using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IOnCommandRejected
    {
        Task OnCommandRejected(ICommand command, string[] messages);
    }
}
