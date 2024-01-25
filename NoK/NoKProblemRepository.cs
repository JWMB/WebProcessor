using NoK.Models;
using NoK.Models.Raw;
using static NoK.NoKProblemRepository;

namespace NoK
{
    public interface IStimuliRepository
    {
        Task<IStimulus?> GetById(string id);
    }

    public interface ISolutionChecker
    {
        Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response);
    }

    public interface ISolutionAnalysis
    {
        bool IsCorrect { get; }
    }

    public interface IHintProvider
    {
    }

    public interface IStimulus
    {
        string Id { get; }
        string SourceId { get; }
    }

    public interface IUserResponse
    {
        string ResponseText { get; }
    }

    public class SimpleUserResponse : IUserResponse
    {
        public string ResponseText { get; set; } = string.Empty;
    }

    public class SimpleSolutionAnalysis : ISolutionAnalysis
    {
        public bool IsCorrect { get; set; }
    }

    public class NoKSolutionChecker : ISolutionChecker
    {
        private readonly NoKProblemRepository repo;

        public NoKSolutionChecker(NoKProblemRepository repo)
        {
            this.repo = repo;
        }

        public async Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response)
        {
            var task = await repo.GetSubtask(stimulus.Id);
            if (task == null)
                throw new Exception($"Task not found: {stimulus.Id}");

            if (task.Answer.Contains(response.ResponseText))
            {
                return new SimpleSolutionAnalysis { IsCorrect = true };
            }
            return new SimpleSolutionAnalysis { IsCorrect = false };
            //if (task.Parent is AssignmentMultiChoice mc)
            //{ }
            //else if (task.Parent is Assignment rg)
            //{ }
            //else
            //    throw new NotImplementedException();
        }
    }

    public class NoKProblemRepository : IStimuliRepository
    {
        public record Config(string AssignmentResource);

        private List<IAssignment> assignments;
        private readonly Config config;

        public NoKProblemRepository(Config config)
        {
            var subparts = RawConverter.ReadRaw(File.ReadAllText(config.AssignmentResource));
            assignments = subparts.SelectMany(o => o.Assignments).ToList();
            this.config = config;
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
                if (index < src.Tasks.Count)
                {
                    task = src.Tasks[index];
                }
                else
                { }
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
            if (src == null)
                return null;
            return new NoKStimulus {
                Presentation = src.Parent?.Body ?? "",
                Question = src.Question ?? "",
                Id = id,
                SourceId = $"{nameof(NoKProblemRepository)};{config.AssignmentResource}",
            };
        }
    }

    public class NoKStimulus : IStimulus
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;

        public string Presentation { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}
