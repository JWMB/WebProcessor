using Common;
using EmailServices;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Text.RegularExpressions;
using static ProblemSourceModule.Services.CreateUserWithTrainings;
using static Tools.BatchCreateUsers;

namespace Tools
{
    public class BatchMail
    {
        private readonly IEmailService emailService;

        public BatchMail(IEmailService emailService)
        {
            this.emailService = emailService;
        }

        public void Send(MailMessage message, string? fromEmailOverride = null)
        {
            if (fromEmailOverride != null)
                message.From = new MailAddress(fromEmailOverride);

            emailService.SendEmail(message);
        }

        public static GmailService CreateGmailService(IConfigurationSection gmailSection)
        {
            var authSection = gmailSection.GetRequiredSection("ClientSecret");
            var app = gmailSection["ApplicationName"];
            if (app == null) throw new ArgumentNullException(app, "ApplicationName");

            return new GmailService(app, authSection.SectionToJson(), "/GMail.AuthTokens");
        }

        public async static Task SendInvitations(IConfiguration config, IEnumerable<CreateUserResult> newUsers, bool actuallySend = false)
        {
            var gmailService = CreateGmailService(config.GetRequiredSection("Gmail"));

            Func<CreateUserResult, Dictionary<string, string>> createReplacements = u =>
                new Dictionary<string, string>
                {
                    { "email", u.User.Email },
                    { "password", u.Password },
                    { "createdGroups", u.CreatedTrainingsToString() },
                };

            await SendTemplated(gmailService, "Vektor invitation", newUsers, u => u.User.Email, createReplacements, from: "jonas.beckeman@gmail.com", actuallySend: actuallySend);
        }

        public static List<string> ReadEmailFile(string emailFile)
        {
            var content = File.ReadAllText(emailFile);
            var dict = SplitToDictionary(new Regex(@"([A-Z]+:)"), content)
                .ToDictionary(o => o.Key, o => o.Value.Split('\n').Select(o => o.Trim()).Where(o => o.Any()).ToList());

            var emails = dict["VALID:"].Except(dict["REJECTED:"]).Except(dict["CHANGED:"]).ToList();

            return emails;
        }

        public static Dictionary<string, string> SplitToDictionary(Regex rx, string input)
        {
            var split = rx.Split(input).Select(o => o.Trim()).Where(o => o.Any()).Select((o, i) => new { Index = i, IsMatch = rx.IsMatch(o), Value = o }).ToList();
            var dict = new Dictionary<string, string>();
            var curr = "";
            foreach (var item in split)
            {
                if (item.IsMatch)
                    curr = item.Value;
                else
                    dict[curr] = item.Value;
            }
            return dict;
        }

        public static async Task SendBatch(GmailService gmailService, string draftName, IEnumerable<string> emailAddresses, string? from = null, bool actuallySend = false)
        {
            await SendTemplated(gmailService, draftName, emailAddresses, o => o, actuallySend: actuallySend);
        }

        public static async Task SendTemplated<T>(GmailService gmailService, string draftName, IEnumerable<T> items, Func<T, string> getEmailAddress,
            Func<T, Dictionary<string, string>>? createReplacements = null, string? from = null, bool actuallySend = false)
        {
            var draft = (await gmailService.GetTemplates(draftName)).FirstOrDefault();
            if (draft == null)
                throw new Exception($"Draft not found: '{draftName}'");

            var fromAddress = draft.From ?? new MailAddress(from ?? "");

            var failedSendingTo = new List<string>();
            foreach (var item in items)
            {
                var body = draft.Body;
                if (createReplacements != null)
                {
                    foreach (var kv in createReplacements(item))
                        body = body.Replace($"{{{kv.Key}}}", kv.Value);
                }

                var msg = new MailMessage(fromAddress.Address, getEmailAddress(item), draft.Subject, body);
                msg.IsBodyHtml = draft.IsBodyHtml;

                var wasSent = false;
                try
                {
                    if (actuallySend)
                    {
                        wasSent = await gmailService.SendEmail(msg);
                    }
                    else
                        wasSent = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                if (!wasSent)
                {
                    failedSendingTo.Add(getEmailAddress(item));
                }
            }
            if (failedSendingTo.Any())
                throw new Exception($"Failed sending to {string.Join(";", failedSendingTo)}");
        }
    }
}
