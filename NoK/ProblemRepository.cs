using NoK.Models.Raw;

namespace NoK
{
    public interface IProblemRepository<T>
    {
        T GetById(string id);
    }

    public class ProblemRepository : IProblemRepository<Problem>
    {
        public record Config(string AssignmentResource);
        public ProblemRepository(Config config)
        {
            var subparts = RawConverter.ReadRaw(File.ReadAllText(config.AssignmentResource));
        }

        public Problem GetById(string id)
        {
            return new Problem();
        }
    }

    public class Problem
    {

    }
}
