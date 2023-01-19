using System.Net.Mail;

namespace EmailServices
{
    public interface IEmailService
    {
        Task<bool> SendEmail(MailMessage data);
    }

    public interface IEmailTemplateProvider
    {
        Task<IEnumerable<MailMessage>> GetTemplates(string? subjectFilter = null);
    }
}