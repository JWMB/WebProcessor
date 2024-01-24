using NoK.Models;
using NoK.Models.Raw;

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
    }

    public class SimpleUserResponse : IUserResponse
    {
        public string Response { get; set; } = string.Empty;
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
            var src = await repo.GetSource(stimulus.Id);
            if (src is AssignmentMultiChoice mc)
            {
                //mc.Task.Answer
            }
            else if (src is Assignment rg)
            {
            }
            else
            {
                throw new NotImplementedException();
            }
            return new SimpleSolutionAnalysis { IsCorrect = true };
        }
    }

    public class NoKProblemRepository : IStimuliRepository
    {
        public record Config(string AssignmentResource);

        private List<IAssignment> assignments;
        public NoKProblemRepository(Config config)
        {
            var subparts = RawConverter.ReadRaw(File.ReadAllText(config.AssignmentResource));
            assignments = subparts.SelectMany(o => o.Assignments).ToList();
        }

        public Task<IAssignment?> GetSource(string id)
        {
            var intId = int.Parse(id);
            return Task.FromResult(assignments.SingleOrDefault(o => o.Id == intId));
        }

        public async Task<IStimulus?> GetById(string id)
        {
            var src = await GetSource(id);
            if (src == null)
                return null;
            return new NoKStimulus();
        }
    }

    public class NoKStimulus : IStimulus
    {
        public string Id { get; set; } = string.Empty;
        public string SourceId => "";

        public string Presentation { get; set; } = string.Empty;
    }
}
