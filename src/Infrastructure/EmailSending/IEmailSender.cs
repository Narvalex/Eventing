using System.Threading.Tasks;

namespace Infrastructure.EmailSending
{
    public interface IEmailSender
    {
        Task Send(string from, string to, string subject, string body, bool esHtml = false);

    }
}
