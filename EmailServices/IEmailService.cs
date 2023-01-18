using System.Net.Mail;

namespace EmailServices
{
    public interface IEmailService
    {
        bool SendEmail(MailMessage data);
    }
}