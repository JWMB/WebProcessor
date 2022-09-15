using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    internal class SyncService
    {
        private readonly HttpClient client;
        private Uri? syncUri;
        public SyncService()
        {
            client = new HttpClient();
        }

        public async Task Init()
        {
            var apiUrl = "https://localhost:7173/api/";

            var response = await client.GetStringAsync($"{apiUrl}Relay/GetSyncUrls?uuid=123");
            syncUri = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SyncUrl>>(response).First().Url;
            if (!syncUri.IsAbsoluteUri)
                throw new Exception($"Sync URI incomplete: {syncUri}");
        }

        public async Task<HttpResponseMessage> Post(object content, string? bearerToken = null)
        {
            if (syncUri == null)
                throw new Exception("Init() has not been called");
            var request = new HttpRequestMessage(HttpMethod.Post, syncUri.AbsoluteUri)
            {
                Content = new StringContent(content is string str ? str : System.Text.Json.JsonSerializer.Serialize(content)),
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken ?? GenerateToken());

            return await client.SendAsync(request);
        }

        private string GenerateToken(string signingKey = "somereallylongkeygoeshere", string audience = "logsink_client", string pipeline = "problemsource")
        {
            var (_, tokenString) = TokenHelper.CreateToken(
                signingKey,
                new TokenHelper.CreateTokenParams
                {
                    Issuer = "jwmb",
                    Audience = audience,
                    Expiry = DateTime.UtcNow.AddHours(1),
                    ClaimsDictionary = new Dictionary<string, string> {
                        { "sub", "klingberglab" },
                        { "pipeline", pipeline },
                    }
                });
            return tokenString;
        }

        public readonly record struct SyncUrl(Uri Url) { }

    }
}
