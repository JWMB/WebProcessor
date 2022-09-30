using ProblemSource.Models.Statistics;

namespace ProblemSource.Services
{
    public interface IPhaseRepository
    {
        Task<List<Phase>> Get(string uuid);
    }

    public class PhaseRepository : IPhaseRepository
    {
        public Task<List<Phase>> Get(string uuid)
        {
            throw new NotImplementedException();
        }
    }
    public class CachingPhaseRepository : IPhaseRepository
    {
        private readonly PhaseRepository inner;

        public CachingPhaseRepository(PhaseRepository inner)
        {
            this.inner = inner;
        }
        public Task<List<Phase>> Get(string uuid)
        {
            return inner.Get(uuid);
        }
    }

}
