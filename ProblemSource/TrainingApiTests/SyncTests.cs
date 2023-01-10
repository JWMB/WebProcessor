using Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using TrainingApiTests.IntegrationHelpers;

namespace WebApi.Tests
{
    public class SyncTests
    {
        private readonly HttpClient client;
        
        public SyncTests()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);

            client = new MyTestServer(services => { })
                .CreateClient();
        }

        [SkippableFact]
        public async Task Sync_Auth_WrongSigningKey()
        {
            var response = await Post("/api/sync/sync", """{ "a": 1 }""", GenerateToken(signingKey: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
        }

        [SkippableFact]
        public async Task Sync_Auth_WrongAudience()
        {
            var response = await Post("/api/sync/sync", """{ "a": 1 }""", GenerateToken(audience: "aa"));
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
        }

        [SkippableFact]
        public async Task Sync_ProblemSource_MissingUserName()
        {
            var response = await Post("/api/sync/sync", """{ "a": 1 }""");
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.ShouldBe("Value cannot be null. (Parameter 'Uuid')");
        }

        [SkippableFact]
        public async Task Sync_ProblemSource_Minimal_OK()
        {
            var uuid = "abc";
            var response = await Post("/api/sync/sync", new
            {
                Uuid = uuid
            });
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("error").GetString().ShouldBe($"Username not found ({uuid})");
        }

        private async Task<HttpResponseMessage> Post(string uri, object content, string? bearerToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(content is string str ? str : System.Text.Json.JsonSerializer.Serialize(content), System.Text.Encoding.UTF8, "application/json"),
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
    }

}