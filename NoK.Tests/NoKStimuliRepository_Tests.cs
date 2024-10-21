using ProblemSourceModule.Services.ProblemGenerators;
using Shouldly;

namespace NoK.Tests
{
    public class NoKStimuliRepository_Tests
    {
        [Fact]
        public async Task GetAndCheckProblem()
        {
			IStimuliRepository problemRepository = new NoKStimuliRepository(new NoKStimuliRepository.Config(Helpers.GetJsonFile("assignments_141094_16961.json")));
            await problemRepository.Init();

            var stimulus = await problemRepository.GetById("141087/0"); // 141087 55224
            stimulus.ShouldNotBeNull();

            var source = await ((NoKStimuliRepository)problemRepository).GetSource("147964");

			var checker = new NoKSolutionChecker((NoKStimuliRepository)problemRepository);
            var analysis = await checker.Check(stimulus, new SimpleUserResponse { ResponseText = "21" });
            analysis.IsCorrect.ShouldBeTrue();
        }
	}
}
