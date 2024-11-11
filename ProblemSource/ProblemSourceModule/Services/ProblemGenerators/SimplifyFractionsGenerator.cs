namespace ProblemSourceModule.Services.ProblemGenerators
{
    public class SimplifyFractionsGenerator : IStimuliGenerator
    {
        private readonly Config config;

        public readonly record struct Config(int NumDecimals = 0, int MinCommonFactors = 1);
        public SimplifyFractionsGenerator(Config config)
        {
            this.config = config;
        }
        public Task<IStimulus> Generate()
        {
            var rnd = new DefaultRNG();
            INumberGenerator primeGenerator = new PrimeGenerator();

            // difficulty: size of lowest common denomitator + size of num and den?

            var primes = primeGenerator.GetAll().Take(5).ToList(); // up to 11
            // more weight on lower primes
            var weightedPrimes = primes
                .Select((o, i) => (WeightFunc(1.0 * i / primes.Count), o))
                .ToList();

            var tools = new RandomCollectionTools(rnd);

            var common = Get(config.MinCommonFactors + rnd.Next(1));

            var numerator = Get(rnd.Next(2)) * common;
            var denominator = Get(rnd.Next(2)) * common;

            return Task.FromResult((IStimulus)new SCDStimulus { Numerator = numerator, Denominator = denominator });

            int Get(int numFactors) => (int)Enumerable.Range(0, numFactors).Concat([1]).Select(o => tools.GetWeightedRandom(weightedPrimes)).Aggregate((p, c) => p * c);
            double WeightFunc(double x) => 1.0 - x; // Math.Pow(x, 0.3);
        }

        public class SCDStimulus : IStimulus
        {
            public decimal Numerator { get; set; }
            public decimal Denominator { get; set; }
            public string Presentation => $"{Numerator} / {Denominator}";

            public string Id => Presentation;

            public string SourceId => string.Empty;

            public override string ToString() => Presentation;
        }
    }
}
