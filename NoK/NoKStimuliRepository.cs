using LLM;
using NoK.Models;
using NoK.Models.Raw;
using ProblemSourceModule.Services.ProblemGenerators;

namespace NoK
{
    public class NoKDomain : IProblemDomain
    {
        private NoKStimuliRepository repo;

        public NoKDomain(NoKStimuliRepository.Config config, ISimpleCompletionService? completionService = null)
        {
            repo = new NoKStimuliRepository(config);
            StimuliRepository = repo;
            solutionChecker = new NoKSolutionChecker(repo, completionService);
        }

        public async Task Init()
        {
            await repo.Init();
        }

        //public ISolutionChecker SolutionChecker { get; init; }
        private ISolutionChecker solutionChecker;
        public ISolutionChecker GetSolutionChecker(string problemId, Type? problemType = null) => solutionChecker;
        public IStimuliRepository StimuliRepository { get; init; }
    }

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

    public static class UriExtensions
    {
        public static Uri WithPath(this Uri uri, string path)
        {
            return new Uri($"{uri.GetSchemeAndHost()}/{path}");
        }
        public static Uri AppendPath(this Uri uri, string path)
        {
            var combined = $"{uri.AbsolutePath}/{path}".Replace("//", "/");
            return new Uri($"{uri.GetSchemeAndHost()}{combined}{uri.Query}");
        }

        public static string GetSchemeAndHost(this Uri uri)
        {
            var port = (uri.Scheme.ToLower() == "https" && uri.Port != 443)
                || (uri.Scheme.ToLower() == "http" && uri.Port != 80)
                ? $":{uri.Port}"
                : "";
            return $"{uri.Scheme.ToLower()}://{uri.DnsSafeHost}{port}";
        }
    }

    public class NoKSolutionChecker : ISolutionChecker
    {
        private readonly NoKStimuliRepository repo;
        private readonly ISimpleCompletionService? completionService;

        public NoKSolutionChecker(NoKStimuliRepository repo, ISimpleCompletionService? completionService = null)
        {
            this.repo = repo;
            this.completionService = completionService;
        }

        public async Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response)
        {
            var task = await repo.GetSubtask(stimulus.Id);
            if (task == null)
                throw new Exception($"Task not found: {stimulus.Id}");

            var isCorrect  = task.CheckResponseIsCorrect(response.ResponseText);
            if (isCorrect == true)
            {
				return new SimpleSolutionAnalysis { IsCorrect = true };
			}

            if (completionService != null)
            {
                var prompt =
    $"""
Din uppgift är att försöka hjälpa användaren att lösa en matteuppgift.

Om användaren gett ett korrekt svar, ska ditt svar inledas med "Korrekt"
Om användaren gett ett felaktigt svar, ska du ge en ledtråd för hur problemet kan lösas, och försöka beskriva vilka misstag användaren verkar ha gjort.
Ge inte det korrekta svaret, ge istället ledtrådar som kan hjälpa användaren att själv lösa problemet.
T.ex., om problemet var "3+5*8" och användaren svarar "64" så kan du svara "Glöm inte att multiplikation går före addition"

Här är problemet som användaren fått:
---
{task.Parent?.Body}
{task.Question}
---

Här är lite material relaterat till problemet:
---
Lösningar:
{string.Join("\n", task.Answer)}

Lösningsförslag:
{string.Join("\n", task.Solution)}

Ledtrådar:
{string.Join("\n", task.Hint)}
---

Användaren svarade med följande:
---
{response.ResponseText}
---

Återigen, lös inte problemet, utan ge användaren tips och ledtrådar om hur man kan tänka för att lösa det!
""";
                var completion = await completionService.GetChatCompletion(prompt);
                if (completion?.Trim().ToLower().StartsWith("korrekt") == true)
                    return new SimpleSolutionAnalysis { IsCorrect = true };
                return new SimpleSolutionAnalysis { IsCorrect = false, Feedback = completion ?? "" };
            }

            return new SimpleSolutionAnalysis { IsCorrect = false };
            //if (task.Parent is AssignmentMultiChoice mc)
            //{ }
            //else if (task.Parent is Assignment rg)
            //{ }
            //else
            //    throw new NotImplementedException();
        }

        public IUserResponse Deserialize(object obj) => IProblemDomain.DeserializeWithId<SimpleUserResponse>(obj);
    }

    public class NoKStimuliRepository : IStimuliRepository
    {
        public record Config(string AssignmentResource);

        private List<IAssignment> assignments = new();
        private Dictionary<int, ContentNode> contentNodes = new();
        private readonly Config config;

        public NoKStimuliRepository(Config config)
        {
            this.config = config;
        }

        public async Task Init()
        {
            var dir = new DirectoryInfo(config.AssignmentResource);
            if (dir.Exists && dir.Extension == "") // for real, DirectoryInfo.Exists is true even if it's a file..?
            {
                assignments = dir.GetFiles("assignment*.json")
                    .Select(o => o.FullName)
                    .Select(File.ReadAllText)
                    .Select(RawConverter.ReadRaw)
                    .SelectMany(o => o.SelectMany(o => o.Assignments))
                    .ToList();
                var allCourses = dir.GetFiles("course*.json")
                    .Select(o => o.FullName)
                    .Select(File.ReadAllText)
                    .Select(Newtonsoft.Json.JsonConvert.DeserializeObject<RawCourse.Root>).OfType<RawCourse.Root>()
                    .Select(ProductNode.Create)
                    .ToList();
                var byId = allCourses.SelectMany(o => o.Descendants().Concat([o]))
                    .GroupBy(o => o.Id);
                var dups = byId.Where(o => o.Count() > 1).ToDictionary(o => o.Key, o => o.ToList());
                contentNodes = byId.ToDictionary(o => o.Key, o => o.First());
            }
            else
            {
                var json = config.AssignmentResource.Contains(":\\")
                    ? File.ReadAllText(config.AssignmentResource)
                    : await new BlobFileFetcher(new BlobFileFetcher.Config("UseDevelopmentStorage=true")).Fetch(config.AssignmentResource);
                //var json = await fetcher.Fetch(config.AssignmentResource); // "nok/assignments_141094_16961/assignments_141094_16961.json");
                var subparts = RawConverter.ReadRaw(json);
                assignments = subparts.SelectMany(o => o.Assignments).ToList();
            }
        }

        public async Task<Subtask?> GetSubtask(string id)
        {
            var src = await GetSource(id); //assignments.SingleOrDefault(o => o.Id == int.Parse(split[0]));
            if (src == null)
                return null;

            var split = id.Split('/');

            Subtask? task = null;
            if (split.Length > 1)
            {
                var index = int.Parse(split[1]);
                task = src.Tasks.SingleOrDefault(o => o.Index == index);
            }
            else if (src.Tasks.Any())
            {
                task = src.Tasks.First();
            }
            return task;
        }

        public Task<IAssignment?> GetSource(string id)
        {
            var split = id.Split('/');
            return Task.FromResult(assignments.SingleOrDefault(o => o.Id == int.Parse(split[0])));
        }

        public async Task<IStimulus?> GetById(string id)
        {
            var src = await GetSubtask(id);
            // TODO: get node
            if (src == null && id.Contains("/") == false)
            {
                if (contentNodes.TryGetValue(int.Parse(id), out var node))
                    return NodeToStimulus(node);
            }
            return src == null ? null : SubtaskToStimulus(src);
        }

        private IStimulus SubtaskToStimulus(Subtask src)
        {
            return new NoKStimulus
            {
                Presentation = $"{(src.Parent?.Body ?? "")}{(src.Parent?.Illustration != null ? $"<img src='{src.Parent?.Illustration}' />" : "")}",
                Question = src.Question ?? "",
                Id = src.Id, // $"{src.Parent.Id}/{src.Index}",
                SourceId = $"{nameof(NoKStimuliRepository)};{config.AssignmentResource}",
            };
        }
        private IStimulus NodeToStimulus(ContentNode node)
        {
            return new NoKStimulus { Id = $"{node.Id}", Presentation = node.OtherContent ?? $"{node.Id}/{node.Name}", Question = string.Empty };
        }

        public Task<List<IStimulus>> GetAll()
        {
            var regular = assignments.OfType<Assignment>().ToList();
            var withAnswers = regular.Where(o => o.Tasks.Any(p => p.Answer.Count == 1)).ToList();
            //var withNonNumericAnswers = withAnswers.SelectMany(o => o.Tasks).Where(o => decimal.TryParse(o.Answer.SingleOrDefault(), out var _) == false).ToList();
            //var withNumericAnswers = withAnswers.Except(withNonNumericAnswers.Select(o => o.Parent)).ToList();
            var toExpose = withAnswers.SelectMany(o => o.Tasks);

            var fromContentNodes = contentNodes.Values.Where(o => string.IsNullOrWhiteSpace(o.OtherContent) == false).Select(NodeToStimulus);

            return Task.FromResult(toExpose.Select(SubtaskToStimulus).Concat(fromContentNodes).ToList()); // toExpose.SelectMany(o => o.Tasks).Select(SubtaskToStimulus).ToList());
        }

        public async Task<List<string>> GetAllIds()
        {
            return (await GetAll()).Select(o => o.Id).ToList(); //withNumericAnswers.SelectMany(o => o.Tasks.Select((_, i) => $"{o.Id}/{i}")).ToList());
        }

        public IStimulus Deserialize(object obj) => IProblemDomain.DeserializeWithId<NoKStimulus>(obj);
    }

    public class NoKStimulus : IStimulus
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;

        public string Presentation { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}
