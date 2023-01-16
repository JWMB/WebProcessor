using System.Net.Mail;

public interface IEmailService
{
    bool SendEmail(MailMessage data);
}
