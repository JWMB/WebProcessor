using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;

namespace BlazorTestBed.Services
{
    public class ApiClient
    {
        private static HttpClient Client = new HttpClient();
        private Uri baseUri;

        public ApiClient(string basePath)
        {
            baseUri = new Uri($"https://localhost:7173/api/{basePath}"); // https://localhost:7173/api"); //  7174 7173 http://localhost:5174/api
        }

        public async Task<T?> Get<T>(string pathAndQuery)
        {
            var tmp = await Client.GetStringAsync($"{baseUri}/{pathAndQuery}");
            return JsonConvert.DeserializeObject<T>(tmp);
        }
        public async Task<T> GetOrThrow<T>(string pathAndQuery)
        {
            var result = await Get<T>(pathAndQuery);
            if (result == null)
                throw new FileNotFoundException();
            return result;
        }

        public async Task<(HttpResponseMessage Response, TResult? Content)> Post<TBody, TResult>(string pathAndQuery, TBody body) where TResult : class
        {
            var response = await Client.PostAsJsonAsync($"{baseUri}/{pathAndQuery}", body);
            var content = await response.Content.ReadAsStringAsync();
            return (response, typeof(TResult) == typeof(string) ? content as TResult: JsonConvert.DeserializeObject<TResult>(content));
        }
    }

    public class ContentTreeService
    {
        private ApiClient Client = new ApiClient("Content");
        public async Task<TreeNodeDto> GetTreeFrom(string? id = null) =>
            await Client.GetOrThrow<TreeNodeDto>($"tree?id={id}");

        public readonly record struct TreeNodeDto(
            string Title,
            string Id,
            string Type,
            List<TreeNodeDto> Children,
            string? Body = null,
            string? Icon = null
            );
    }

    public class StimuliResponseService
    {
        private ApiClient Client = new ApiClient("StimuliResponse");

        public async Task<Stimulus> GetStimuli(string id) =>
            await Client.GetOrThrow<Stimulus>($"?id={id}");

        public async Task<List<IdAndSummary>> GetAllStimuliSummaries() =>
            (await Client.Get< List<IdAndSummary>>($"summaries?source=xxx")) ?? new();

        public async Task<(HttpStatusCode, string)> SendResponse(UserResponse userResponse)
        {
            var result = await Client.Post<UserResponse, string>($"", userResponse);
            return (result.Response.StatusCode, result.Content ?? string.Empty);
        }

        public class UserResponse
        {
            public string Id { get; set; } = "";
            public string SourceId { get; set; } = "";
            public string ResponseText { get; set; } = "";
        }

        public record IdAndSummary(string Id, string Summary);
        public class Stimulus
        {
            public string Id { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string Question { get; set; } = string.Empty;
            //{"id":"141087/0","sourceId":"NoKStimuliRepository;C:\\Users\\jonas\\Downloads\\assignments_141094_16961\\assignments_141094_16961.json","presentation":"<p><style type=\"text/css\"></style><i>Lös uppgiften utan digitalt verktyg.</i></p><p>Beräkna</p>","question":"`(7*60)/20`"}
        }

    }
}
