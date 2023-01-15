// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Text;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .Build();

var gmailSection = config.GetRequiredSection("Gmail");
var authSection = gmailSection.GetRequiredSection("ClientSecret");
var app = gmailSection["ApplicationName"];
if (app == null) throw new ArgumentNullException(app, "ApplicationName");

var emailService = new GmailService(app, SectionToJson(authSection), "/authtokens");

// 128906761120-q5ut67ddu7n6jh8pa9phbktlofj5j1qh.apps.googleusercontent.com
emailService.SendEmail(new MailMessage("jonas.beckeman@gmail.com", "jonas.beckeman@outlook.com", "Test", "Here's a test message"));

static string SectionToJson(IConfigurationSection section)
{
    var entries = new Dictionary<string, string>();

    var isList = section.GetChildren().All(o => int.TryParse(o.Key, out var _));

    foreach (var child in section.GetChildren())
    {
        var value = child.Value != null
            ? $"\"{child.Value}\""
            : SectionToJson(child);

        entries.Add(child.Key, value);
    }

    return isList
        ? $"[{string.Join(",", entries.OrderBy(o => int.Parse(o.Key)).Select(o => o.Value))}]"
        : $"{{{string.Join(",", entries.Select(o => $"\"{o.Key}\": {o.Value}"))}}}";
}