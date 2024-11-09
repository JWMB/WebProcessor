using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;

namespace BlazorTestBed.Services
{
    public class StimuliResponseService
    {
        private Uri baseUri = new Uri(""); //https://localhost:7173/api  7174 7173 http://localhost:5174/api

        public StimuliResponseService()
        {
        }

        private HttpClient Client => new HttpClient();


        public async Task<Stimulus> GetStimuli(string id)
        {
            var tmp = await Client.GetStringAsync($"{baseUri}/StimuliResponse?id={id}");
            return JsonConvert.DeserializeObject<Stimulus>(tmp);
        }
        public async Task<List<IdAndSummary>> GetAllStimuliSummaries()
        {
            var tmp = await Client.GetStringAsync($"{baseUri}/StimuliResponse/summaries?source=xxx");
            return JsonConvert.DeserializeObject<List<IdAndSummary>>(tmp) ?? new();
        }
        public async Task<(HttpStatusCode, string)> SendResponse(UserResponse userResponse)
        {
            var tmp = await Client.PostAsJsonAsync($"{baseUri}/StimuliResponse", userResponse);
            return (tmp.StatusCode, await tmp.Content.ReadAsStringAsync());
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
