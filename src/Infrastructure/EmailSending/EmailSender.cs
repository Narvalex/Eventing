using Infrastructure.Utils;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Infrastructure.EmailSending
{
    public class EmailSender : IEmailSender
    {
        private readonly string host;
        private readonly int port;
        private readonly string user;
        private readonly string password;
        private readonly bool enableSsl;

        public EmailSender(string host, int port, string user, string password, bool enableSsl)
        {
            this.host = Ensured.NotEmpty(host, nameof(host));
            this.port = Ensured.NotNegative(port, nameof(port));
            this.user = Ensured.NotEmpty(user, nameof(user));
            this.password = Ensured.NotEmpty(password, nameof(password));
            this.enableSsl = enableSsl;
        }

        public async Task Send(string from, string to, string subject, string body, bool esHtml = false)
        {
            using (var smtpClient = this.CreateSmtpClilent())
            using (var email = new MailMessage(from, to, subject, body))
            {
                email.IsBodyHtml = esHtml;
                await smtpClient.SendMailAsync(email);
            }
        }

        private SmtpClient CreateSmtpClilent()
        {
            return new SmtpClient(this.host, this.port)
            {
                EnableSsl = this.enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(this.user, this.password)
            };
        }
    }
}
