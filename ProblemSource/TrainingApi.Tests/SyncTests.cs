using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProblemSource.Models;
using ProblemSourceModule.Models;
using Shouldly;
using System.Net.Http.Json;
using System.Text.Json;
using TrainingApi.Tests.IntegrationHelpers;

namespace TrainingApi.Tests
{
    public class SyncTests
    {
        private readonly HttpClient client;
        
        public SyncTests()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);

            client = new MyTestServer(
                null,
                services => { },
                config: new Dictionary<string, string> { { "AppSettings:AzureTables:TablePrefix", "vektorTEST" } }
                ).CreateClient();
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
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("error").GetString().ShouldBe($"Username not found ({uuid})");
        }

        [SkippableFact]
        public void CreateToken()
        {
            var token = GenerateToken(expiry: TimeSpan.FromDays(365 * 5));
        }


        [SkippableFact]
        public async Task Sync_ProblemSource_XXX()
        {
            var uuid = "nesi kipomebi";
            var response = await Post("/api/sync/sync", new SyncInput
            {
                Uuid = uuid,
                RequestState = true,
            });

            var result = await response.Content.ReadFromJsonAsync<SyncResult>();
            if (result == null)
                throw new Exception("");

            var fullState = JsonConvert.DeserializeObject<UserFullState>(result.state);
            if (fullState == null)
                throw new Exception("");

            var dp = DynamicTrainingPlan.Create((JObject)JObject.Parse(result.state)["training_plan"]!);

            PlanetBundler.init();
            var planets = PlanetBundler.getPlanets(fullState.exercise_stats, fullState.training_settings, dp, true)
                .OrderBy(o => o.lastUsed);

            var definedGames = dp.getDefinedGames();
            var availableGames = dp.getAvailableGames(fullState.exercise_stats);
            var proposed = dp.getProposedGames(new List<string>(), fullState.exercise_stats);

            var tpOverrides = fullState.training_settings.trainingPlanOverrides;
            dp.changeWeights(fullState.exercise_stats, new Dictionary<string, decimal> { });
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

        private string GenerateToken(string signingKey = "somereallyreallyreallylongkeygoeshere", string audience = "logsink_client", string pipeline = "problemsource", TimeSpan? expiry = null)
        {
            var (_, tokenString) = TokenHelper.CreateToken(
                signingKey,
                new TokenHelper.CreateTokenParams
                {
                    Issuer = "jwmb",
                    Audience = audience,
                    Expiry = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1)),
                    ClaimsDictionary = new Dictionary<string, string> {
                        { "sub", "klingberglab" },
                        { "pipeline", pipeline },
                    }
                });
            return tokenString;
        }
    }

}