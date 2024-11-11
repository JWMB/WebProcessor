namespace ProblemSourceModule.Services.ProblemGenerators
{
    public class SimplifyFractionsGenerator : IStimuliGenerator
    {
        private readonly Config config;

        public readonly record struct Config(int NumDecimals = 0);
        public SimplifyFractionsGenerator(Config config)
        {
            this.config = config;
        }
        public Task<IStimulus> Generate()
        {
            throw new NotImplementedException();
            var rnd = new Random();
            INumberGenerator primeGenerator = new PrimeGenerator();

            // difficulty: size of lowest common denomitator + size of num and den?
            var primes = primeGenerator.GetAll().Take(5).ToList(); // up to 11
            // TODO: more weight on lower primes

            return Task.FromResult((IStimulus)new SCDStimulus { Numerator = 1, Denominator = 1 });

            T GetWeightedRandom<T>(List<(double weight, T value)> weighted)
            {
                var total = 0.0;
                var sums = new List<double>();
                foreach (var kvp in weighted)
                {
                    total += kvp.weight;
                    sums.Add(total);
                }
                var rnd = new Random();
                var r = rnd.NextDouble() * sums.Last();
                var index = sums.FindIndex(o => o > r) - 1;
                return weighted[index].value;
            }

            IEnumerable<int> GetFrom(List<int> list, int count, bool unique = false)
            {
                var rnd = new Random();
                if (unique)
                {
                    if (count > list.Count)
                        throw new Exception("");
                    var copy = new List<int>(list);
                    var result = new List<int>();
                    for (int i = 0; i < count; i++)
                    {
                        var index = rnd.Next(copy.Count);
                        result.Add(copy[index]);
                        copy.RemoveAt(index);
                    }
                    return result;
                }
                else
                {
                    return Enumerable.Range(0, count).Select(o => list[rnd.Next(list.Count)]);
                }
            }
        }
        public class SCDStimulus : IStimulus
        {
            public decimal Numerator { get; set; }
            public decimal Denominator { get; set; }
            public string Presentation => $"{Numerator} / {Denominator}";

            public string Id => Presentation;

            public string SourceId => string.Empty;
        }
    }
}
