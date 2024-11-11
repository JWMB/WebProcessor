using ProblemSourceModule.Services.ProblemGenerators;
using Shouldly;

namespace ProblemSourceModule.Tests
{
    public class DivGeneratorTests
    {
        [Fact]
        public void PrimeGenerator_Sequence()
        {
            PrimeGenerator.Generate().Take(10).Select(o => (int)o).ShouldBe([2, 3, 5, 7, 11, 13, 17, 19, 23, 29]);
        }

        [Theory]
        [InlineData(1, new int[] { })]
        [InlineData(4, new[] { 2, 2 })]
        [InlineData(54, new[] { 2, 3, 3, 3 })]
        [InlineData(1463, new[] { 7, 11, 19 })]
        public void PrimeGenerator_GetConstituents(int value, IEnumerable<int> expected)
        {
            PrimeGenerator.GetPrimeConstituents(value).ShouldBe(expected);
        }

        [Fact]
        public void GetWeightedRandom()
        {
            var sut = new RandomCollectionTools(new DefaultRNG());
            var weighted = new List<(double, string)> { (1.0, "a"), (1, "b"), (2, "c"), (0.5, "d") };

            var numIterations = 1000;
            var selected = Enumerable.Range(0, numIterations)
                .Select(o => sut.GetWeightedRandom(weighted))
                .GroupBy(o => o)
                .Select(o => (o.Count() * 1.0, o.Key))
                .ToList();
        }

        [Fact]
        public async Task SimplifyFractionsGenerator()
        {
            var sut = new SimplifyFractionsGenerator(new());
            var result = await sut.Generate();
        }

        [Theory]
        [InlineData(10, 2, "5/1")]
        [InlineData(2.4, 2.1, "8/7")]
        [InlineData(1650, 330, "5/1")]
        public async Task SimplifyFractionsChecker(decimal num, decimal den, string response)
        {
            var sut = new SimplifyFractionsGenerator.SFSolutionChecker();

            var result = await sut.Check(
                new SimplifyFractionsGenerator.SFStimulus {  Numerator = num, Denominator = den },
                new SimpleUserResponse { ResponseText = response });

            result.IsCorrect.ShouldBeTrue();
        }
    }
}
