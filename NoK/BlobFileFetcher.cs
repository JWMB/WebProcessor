namespace NoK
{
    public class BlobFileFetcher
    {
        private readonly Config config;

        public record Config(string connectionString);
        public BlobFileFetcher(Config config)
        {
            this.config = config;
        }

        public async Task<string> Fetch(string path)
        {
            var baseUri = config.connectionString.StartsWith("UseDevelopmentStorage") ? "http://127.0.0.1:10000/devstoreaccount1" : config.connectionString;
            var url = new Uri(baseUri);
            //var accountName = url.AbsolutePath.Trim('/');
            url = url.AppendPath(path);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            //request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            //    "SharedKey",
            //    $"{accountName}:{key}");
            request.Headers.Date = DateTimeOffset.UtcNow;
            //x-ms-version
            using var client = new HttpClient();
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            throw new Exception(response.StatusCode.ToString());
        }
    }
}
