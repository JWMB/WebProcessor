using NoK.Models;
using NoK.Models.Raw;
using ProblemSourceModule.Services.ProblemGenerators;

namespace NoK
{
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

        public IEnumerable<ContentNode> ContentNodes => contentNodes.Values;
        //public IStimulus Deserialize(object obj) => IProblemDomain.DeserializeWithId<NoKStimulus>(obj);
    }
}
