using System.Threading.Tasks;

namespace Infrastructure.Messaging.Handling
{
    public interface IOnQueryRejected
    {
        Task OnQueryRejected(IQuery query, string[] messages);
    }
}
