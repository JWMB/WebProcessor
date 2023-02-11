using Common;
using EmailServices;
using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
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

        public static MailMessage CreateNewUserCreatedMessage(CreateUserResult createUserResult)
        {
            var urlTraining = new Uri("https://kistudyclient.azurewebsites.net/");
            var urlAdmin = new Uri("https://kistudysync.azurewebsites.net/admin/index.html");

            var trainingInfo = $@"
<p>To log in with a training account, navigate to {urlTraining.AbsoluteUri} and enter the training username.</p>
<p>Note: In order to make the experience more app-like, you can enable Progressive Web App (PWA) mode by following these instructions: https://mobilesyrup.com/2020/05/24/how-install-progressive-web-app-pwa-android-ios-pc-mac/</p>
";
            var createdTrainingsInfo = createUserResult.CreatedTrainings.Any()
                ? $"<p>We have created the following test training logins for you:\n{createUserResult.CreatedTrainingsToString()}</p>\n\n{trainingInfo}"
                : "";

            var body = $@"
<p>Hej, 
Vi har glädjen att meddela att Vektor återuppstår, med hjälp av forskningsfinansiering från en Wallenbergstiftelse och med bas i Karolinska Institutet.</p>

Appen ser ut och fungerar precis som förut. Liksom tidigare kommer den innehålla det träningsschema som vi funnit bäst baserat på tidigare forskning. 
Vi kommer också variera några procent av tiden för att genom analyser kunna göra appen ännu bättre i framtiden.
Vi kommer inte spara några personliga data och användandet av appen är godkänt av etikprövningsmyndigheten.
";

            body = $@"
<p>Hello,</p>

<p>a Vektor Teacher account has been created for you (email address {createUserResult.User.Email}) with password '{createUserResult.Password}'.</p>

<p>Please navigate to {urlAdmin.AbsoluteUri} and click 'Log in' using the above credentials.</p>

{createdTrainingsInfo}

<p>**NOTE**</p>
<p>The user interface for the Teacher site is in a very early preview stage. Expect it to change quite often during early 2023.</p>
";
            var msg = new MailMessage("noreply@test.com", createUserResult.User.Email, "Vektor invitation", body.Trim());
            msg.IsBodyHtml = true;
            return msg;
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

            await SendTemplated(gmailService, "jonas.beckeman@gmail.com", "Vektor invitation", newUsers, u => u.User.Email, createReplacements, actuallySend);
        }

        public static async Task SendTemplated<T>(GmailService gmailService, string from, string draftName, IEnumerable<T> items, Func<T, string> getEmail,
            Func<T, Dictionary<string, string>> createReplacements, bool actuallySend = false)
        {
            var draft = (await gmailService.GetTemplates(draftName)).FirstOrDefault();
            if (draft == null)
                throw new Exception($"Draft not found: '{draftName}'");

            var failedSendingTo = new List<string>();
            foreach (var item in items)
            {
                var body = draft.Body;
                foreach (var kv in createReplacements(item))
                    body = body.Replace($"{{{kv.Key}}}", kv.Value);

                var msg = new MailMessage(from, getEmail(item), draft.Subject, body);
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
                }
                if (!wasSent)
                {
                    failedSendingTo.Add(getEmail(item));
                }
            }
            if (failedSendingTo.Any())
                throw new Exception($"Failed sending to {string.Join(";", failedSendingTo)}");
        }
    }
}
