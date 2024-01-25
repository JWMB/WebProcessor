using Shouldly;

namespace NoK.Tests
{
    public class Class1
    {
        [Fact]
        public async Task X()
        {
            IStimuliRepository problemRepository = new NoKProblemRepository(new NoKProblemRepository.Config(@"C:\Users\jonas\Downloads\assignments_141094_16961\assignments_141094_16961.json"));

            var stimulus = await problemRepository.GetById("141087/0"); // 141087 55224
            stimulus.ShouldNotBeNull();

            var checker = new NoKSolutionChecker((NoKProblemRepository)problemRepository);
            var analysis = await checker.Check(stimulus, new SimpleUserResponse { ResponseText = "21" });
            analysis.IsCorrect.ShouldBeTrue();
        }
    }
}
