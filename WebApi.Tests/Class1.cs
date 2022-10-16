using Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace WebApi.Tests
{
    public class Class1
    {
        private readonly HttpClient client;

        public Class1()
        {
            var application = new WebApplicationFactory<WebApi.Program>()
                .WithWebHostBuilder(builder =>
                {
                    // ... Configure test services
                });

            client = application.CreateClient();
        }

        [Fact]
        public async Task Sync_Auth_WrongSigningKey()
        {
            var response = await Post("/api/sync/sync", "{ \"a\": 1 }", GenerateToken(signingKey: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Sync_Auth_WrongAudience()
        {
            var response = await Post("/api/sync/sync", "{ \"a\": 1 }", GenerateToken(audience: "aa"));
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Sync_ProblemSource_MissingApiKey()
        {
            var response = await Post("/api/sync/sync", "{ \"a\": 1 }");
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Sync_ProblemSource_Minimal_OK()
        {
            var response = await Post("/api/sync/sync", new
            {
                //ApiKey = "abc",
                Uuid = "abc"
            });
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        private async Task<HttpResponseMessage> Post(string uri, object content, string? bearerToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
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
    }

}