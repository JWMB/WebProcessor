using LLM;
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
}
