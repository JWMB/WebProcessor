using static TrainingApi.Controllers.TrainingsController;

namespace LoadTester
{
    internal class AdminClient
    {
        private readonly Config config;

        public record Config(Uri BaseUrl, string AuthCookie);
        public AdminClient(Config config)
        {
            this.config = config;
        }

        public async Task<IEnumerable<string>> GetUsernames(string group)
        {
            using var client = new HttpClient();

            var summaries = await GetGroupTrainings(group);

            foreach (var trainingId in summaries.Where(o => o.TrainedDays > 0).Select(o => o.Id))
            {
                var req = new HttpRequestMessage(HttpMethod.Delete, GetUri($"trainings?id={trainingId}"));
                AddAuth(req);
                var response = await client.SendAsync(req);
                response.EnsureSuccessStatusCode();
            }
            return summaries.Select(o => o.Username);

            async Task<List<TrainingSummaryWithDaysDto>> GetGroupTrainings(string group)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, GetUri($"Trainings/summaries?group={System.Net.WebUtility.UrlEncode(group)}"));
                AddAuth(req);
                var response = await client.SendAsync(req);
                response.EnsureSuccessStatusCode();
                var asString = await response.Content.ReadAsStringAsync();
                List<TrainingSummaryWithDaysDto>? result;
                try
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<List<TrainingSummaryWithDaysDto>>(asString, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // await response.Content.ReadFromJsonAsync<List<TrainingSummary>>();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                if (result == null)
                    throw new NullReferenceException();

                return result;
            }

            Uri GetUri(string path)
            {
                return new Uri(config.BaseUrl, path);
            }
            void AddAuth(HttpRequestMessage m)
            {
                if (!string.IsNullOrEmpty(config.AuthCookie))
                    m.Headers.Add("Cookie", $".AspNetCore.Cookies={config.AuthCookie}");
            }
        }

    }
}
