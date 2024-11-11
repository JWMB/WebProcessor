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
    }
}
