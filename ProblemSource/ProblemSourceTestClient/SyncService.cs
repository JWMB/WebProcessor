using Common;

namespace TestClient
{
    internal class SyncService
    {
        private readonly IHttpClientFactory clientFactory;
        private Uri? syncUri;

        public SyncService(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        private HttpClient GetClient() => clientFactory.CreateClient();

        public async Task Init(string baseUrl)
        {
            var response = await GetClient().GetStringAsync($"{baseUrl}Relay/GetSyncUrls?uuid=123");
            syncUri = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SyncUrl>>(response)?.First().Url;
            if (syncUri?.IsAbsoluteUri == false)
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

            return await GetClient().SendAsync(request);
        }

        private string GenerateToken(string signingKey = "somereallyreallyreallylongkeygoeshere", string audience = "logsink_client", string pipeline = "problemsource")
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
