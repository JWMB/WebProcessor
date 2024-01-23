using NoK.Models;
using NoK.Models.Raw;

namespace NoK
{
    public interface IProblemRepository<T>
    {
        T GetById(string id);
    }

    public interface ISolutionChecker<T>
    {
        ISolutionAnalysis<T> Check(T problem);
    }
    public interface ISolutionAnalysis<T>
    {
        bool IsCorrect { get; }
    }

    public class ProblemRepository : IProblemRepository<Problem>
    {
        public record Config(string AssignmentResource);

        private List<IAssignment> assignments;
        public ProblemRepository(Config config)
        {
            var subparts = RawConverter.ReadRaw(File.ReadAllText(config.AssignmentResource));
            assignments = subparts.SelectMany(o => o.Assignments).ToList();
        }

        public Problem GetById(string id)
        {
            var intId = int.Parse(id);
            var assignment = assignments.SingleOrDefault(o => o.Id == intId);
            return new Problem();
        }
    }

    public class Problem : IStimulus
    {
        public string Id { get; set; }
        public string Type => "";

        public string Html { get; set; }
    }

    public interface IStimulus
    {
        string Id { get; }
        string Type { get; }
    }
}
