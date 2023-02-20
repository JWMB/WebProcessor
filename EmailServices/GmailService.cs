// See https://aka.ms/new-console-template for more information
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Net.Mail;

namespace EmailServices
{
    public class GmailService : IEmailService, IEmailTemplateProvider
    {
        private readonly string applicationName;
        private readonly string settingsJson;
        private readonly string pathTokenResponseFolder;

        private UserCredential? userCredential;

        // https://mycodebit.com/send-emails-in-asp-net-core-5-using-gmail-api/
        // https://console.cloud.google.com/
        public GmailService(string applicationName, string settingsJson, string pathTokenResponseFolder)
        {
            this.applicationName = applicationName;
            this.settingsJson = settingsJson;
            this.pathTokenResponseFolder = pathTokenResponseFolder;
        }

        private UserCredential CreateCredential()
        {
            if (userCredential != null)
            {
                if (userCredential.Token.ExpiresInSeconds.HasValue == false)
                {
                    return userCredential;
                }
                else
                {
                    if (DateTime.UtcNow > userCredential.Token.IssuedUtc.AddSeconds(userCredential.Token.ExpiresInSeconds.Value))
                    {
                        var success = userCredential.RefreshTokenAsync(CancellationToken.None).Result;
                        if (success)
                            return userCredential;
                    }
                }
            }

            var stream = new MemoryStream();
            stream.Write(System.Text.Encoding.UTF8.GetBytes(settingsJson));
            stream.Position = 0;

            userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                         GoogleClientSecrets.FromStream(stream).Secrets,
                          new[] {
                              // https://developers.google.com/gmail/api/auth/scopes
                              // https://console.cloud.google.com/
                              // Note: after modifying, might have to delete local token
                              // Seems to be incorrect: https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth
                              // Could find no subfolder in %APPDATA% with the pathTokenResponseFolder name 
                              // Easiest is to pick a different name for pathTokenResponseFolder
                              Google.Apis.Gmail.v1.GmailService.Scope.GmailSend,
                              Google.Apis.Gmail.v1.GmailService.Scope.GmailCompose
                          },
                          "user",
                          CancellationToken.None,
                          new FileDataStore(pathTokenResponseFolder, true)).Result;
            return userCredential;
        }

        private Google.Apis.Gmail.v1.GmailService CreateService()
        {
            var credential = CreateCredential();
            return new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }

        public static string CreateMimeBody(MailMessage data)
        {
            var rnd = new Random();
            var boundary = string.Join("", Enumerable.Range(0, 26).Select(o => (char)rnd.Next(65, 90)));
            var body = data.IsBodyHtml == false ? GetPlainText(data.Body)
                : $@"
Content-Type: multipart/alternative; boundary=""{boundary}""

--{boundary}
{GetPlainText(RemoveHtml(data.Body))}

--{boundary}
Content-Type: text/html;charset=utf-8

{data.Body}

--{boundary}
".Trim();

            return body;
            string RemoveHtml(string html)
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument("<html><body></body></html>");
                var nodes = new HtmlParser().ParseFragment(html, dom.Body!);
                return string.Concat(nodes.Select(o => o.Text()));
            }

            string GetPlainText(string body) =>
                $@"
Content-Type: text/plain;charset=utf-8

{body}
".Trim();
        }

        public static string CreateMimeMessage(MailMessage data)
        {
            // Date: 
            // Message-Id: 
            var message = $@"
From: {data.From ?? new MailAddress("")}
To: {data.To}
Subject: {data.Subject}
MIME-Version: 1.0
{CreateMimeBody(data)}
".Trim();
            return message.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private Message CreateMessage(MailMessage data)
        {
            var newMsg = new Message();
            newMsg.Raw = Base64UrlEncode(CreateMimeMessage(data));
            return newMsg;
        }

        private static string MeUser = "me";

        public Task<bool> SendEmail(MailMessage data)
        {
            var msg = CreateMessage(data);
            var service = CreateService();

            var onlyDraft = false;
            Message response;
            if (onlyDraft)
            {
                response = service.Users.Messages.Insert(msg, MeUser).Execute(); // Request had insufficient authentication scopes.'
            }
            else
            {
                response = service.Users.Messages.Send(msg, MeUser).Execute();
                if (response?.LabelIds.Contains("SENT") == true)
                    return Task.FromResult(true);
            }
            // TODO: async variant doesn't work? var response = await service.Users.Messages.Send(msg, MeUser).ExecuteAsync();

            throw new Exception($"{(response == null ? "null" : "labels: " + string.Join(", ", response.LabelIds))}");
        }

        private static string Base64UrlEncode(string input)
        {
            // Special "url-safe" base64 encode.
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input))
              .Replace('+', '-')
              .Replace('/', '_')
              .Replace("=", "");
        }

        private static string Base64UrlDecode(string input)
        {
            input = input
                .Replace('-', '+')
                .Replace('_', '/');
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(input));
        }

        public async Task<IEnumerable<MailMessage>> GetTemplates(string? subjectFilter = null)
        {
            var service = CreateService();
            var cmd = service.Users.Drafts.List(MeUser);
            cmd.MaxResults = 5;
            if (subjectFilter != null)
            {
                cmd.Q = $"subject: {subjectFilter}";
            }
            var response = await cmd.ExecuteAsync();

            var drafts = new List<Draft>();
            foreach (var item in response.Drafts)
            {
                var draft = await service.Users.Drafts.Get(MeUser, item.Id).ExecuteAsync();
                if (draft != null)
                    drafts.Add(draft);
            }

            var result = drafts
                .Where(o => o.Message.Payload != null)
                .Select(o => CreateFrom(o.Message))
                .OfType<MailMessage>()
                .ToList();

            return result;
        }

        public static MailMessage? CreateFrom(Message message)
        {
            // message.LabelIds ["DRAFT"]

            // { "name": "MIME-Version", "value": "1.0", "ETag": null },
            // { "name": "Date", "value": "Tue, 17 Jan 2023 16:27:34 +0100", "ETag": null },
            // { "name": "Message-ID", "value": "<CAKGQKycHO8ens9Gz2O2tSujncxgdvz2qRhWrBmftVvMbimM_3Q@mail.gmail.com>", "ETag": null },
            // { "name": "Subject", "value": "", "ETag": null },
            // { "name": "From", "value": "Jonas Beckeman <jonas.beckeman@gmail.com>", "ETag": null },
            // { "name": "Content-Type", "value": "multipart/alternative; boundary=\"000000000000d8cc2705f27756cc\"", "ETag": null }

            if (message.Payload == null)
                return null;

            var fallbackAddress = "unknown@gmail.com";
            var result = new MailMessage(GetHeader("From", fallbackAddress), GetHeader("To", fallbackAddress), GetHeader("Subject", ""), "");

            MessagePart? part;
            {
                part = message.Payload.Parts?.FirstOrDefault(o => o.MimeType == "text/html");
                if (part != null)
                {
                    result.IsBodyHtml = true;
                }
                else
                {
                    part = message.Payload.Parts?.FirstOrDefault(o => o.MimeType == "text/plain");
                    result.IsBodyHtml = false;
                }
            }

            if (part == null)
                return null;

            result.Body = Base64UrlDecode(part.Body.Data);

            AddAddresses("Bcc", result.Bcc);
            AddAddresses("Cc", result.CC);

            return result;

            void AddAddresses(string field, MailAddressCollection collection)
            {
                var data = GetHeaderOrNull(field);
                if (data != null)
                {
                    var split = data.Split(',').Select(o => o.Trim()).Where(o => o.Length > 2);
                    foreach (var item in split)
                    {
                        collection.Add(new MailAddress(item));
                    }
                }
            }

            string GetHeader(string key, string defaultValue) =>
                message.Payload.Headers.FirstOrDefault(o => o.Name == key)?.Value ?? defaultValue;

            string? GetHeaderOrNull(string key) =>
                message.Payload.Headers.FirstOrDefault(o => o.Name == key)?.Value ?? null;
        }
    }
}