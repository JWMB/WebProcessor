// See https://aka.ms/new-console-template for more information
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Net.Mail;

public class GmailService : IEmailService
{
    private readonly string applicationName;
    private readonly string settingsJson;
    private readonly string pathTokenResponseFolder;

    private UserCredential? userCredential;

    // https://mycodebit.com/send-emails-in-asp-net-core-5-using-gmail-api/
    public GmailService(string applicationName, string settingsJson, string pathTokenResponseFolder)
    {
        this.applicationName = applicationName;
        this.settingsJson = settingsJson;
        this.pathTokenResponseFolder = pathTokenResponseFolder;
    }

    private UserCredential CreateCredential()
    {
        if (userCredential != null) {
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
                      new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend },
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

    public bool SendEmail(MailMessage data)
    {
        var msg = CreateMessage(data);
        try
        {
            var service = CreateService();
            var response = service.Users.Messages.Send(msg, "me").Execute();
            if (response?.LabelIds.Contains("SENT") == true)
                return true;
            //service.Users.Messages.Insert()

            throw new Exception($"{(response == null ? "null" : "labels: " + string.Join(", ", response.LabelIds))}");
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    private static string Base64UrlEncode(string input)
    {
        // Special "url-safe" base64 encode.
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input))
          .Replace('+', '-')
          .Replace('/', '_')
          .Replace("=", "");
    }
}