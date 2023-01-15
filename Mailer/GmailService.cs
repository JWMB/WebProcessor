// See https://aka.ms/new-console-template for more information
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

    // https://mycodebit.com/send-emails-in-asp-net-core-5-using-gmail-api/
    public GmailService(string applicationName, string settingsJson, string pathTokenResponseFolder)
    {
        this.applicationName = applicationName;
        this.settingsJson = settingsJson;
        this.pathTokenResponseFolder = pathTokenResponseFolder;
    }

    private UserCredential CreateCredential()
    {
        var stream = new MemoryStream();
        stream.Write(System.Text.Encoding.UTF8.GetBytes(settingsJson));
        stream.Position = 0;

        return GoogleWebAuthorizationBroker.AuthorizeAsync(
                     GoogleClientSecrets.FromStream(stream).Secrets,
                      new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend },
                      "user",
                      CancellationToken.None,
                      new FileDataStore(pathTokenResponseFolder, true)).Result;
    }

    public bool SendEmail(MailMessage data)
    {
        try
        {
            var credential = CreateCredential();

            var service = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            var message = $"To: {data.To}\r\nSubject: {data.Subject}\r\nContent-Type: text/html;charset=utf-8\r\n\r\n{data.Body}";
            var newMsg = new Message();
            newMsg.Raw = Base64UrlEncode(message);
            var response = service.Users.Messages.Send(newMsg, "me").Execute();
            if (response?.LabelIds.Contains("SENT") == true)
                return true;

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