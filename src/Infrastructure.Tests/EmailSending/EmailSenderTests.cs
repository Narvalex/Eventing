using Infrastructure.EmailSending;
using System.Threading.Tasks;

namespace Infrastructure.Tests
{
    public class given_email_sender
    {
        private readonly EmailSender sut;
        /// <summary>
        /// https://aspnetcoremaster.com/.net/smtp/smptclient/dotnet/2018/07/22/enviar-un-correo-con-csharp-gmail.html
        /// </summary>
        public given_email_sender()
        {
            this.sut = new EmailSender("smtp.gmail.com",587,"gicaldev@gmail.com","Gicaldev2018./",true);
        }

        //[Fact]
        public async Task when_sending_mail_then_mail_is_sent()
        {
            var mensaje = "este es un super mensaje desde el infinito y mas alla";
            await this.sut.Send("gicaldev@gmail.com","nestor@gicaldev.com.py","Prueba de envio de correo", mensaje);
        }
    }
}
