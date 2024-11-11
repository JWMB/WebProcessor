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

            return Task.FromResult((IStimulus)new SFStimulus { Numerator = numerator, Denominator = denominator });

            int Get(int numFactors) => (int)Enumerable.Range(0, numFactors).Concat([1]).Select(o => tools.GetWeightedRandom(weightedPrimes)).Aggregate((p, c) => p * c);
            double WeightFunc(double x) => 1.0 - x; // Math.Pow(x, 0.3);
        }

        public static SFStimulus Simplify(SFStimulus stimulus)
        {
            var scale = Math.Max(stimulus.Numerator.Scale, stimulus.Denominator.Scale);
            var factToInts = (int)Math.Pow(10, scale);
            var primesNum = PrimeGenerator.GetPrimeConstituents((int)(stimulus.Numerator * factToInts));
            var primesDenom = PrimeGenerator.GetPrimeConstituents((int)(stimulus.Denominator * factToInts));

            var common = primesNum.Intersect(primesDenom).ToList();
            var divideBy = 1.0M / factToInts * common.Aggregate((p, c) => p * c);

            return new SFStimulus { Numerator = (int)(stimulus.Numerator / divideBy), Denominator = (int)(stimulus.Denominator / divideBy) };
        }

        public class SFStimulus : IStimulus
        {
            public decimal Numerator { get; set; }
            public decimal Denominator { get; set; }
            public string Presentation => $"{Numerator} / {Denominator}";

            public string Id => Presentation;

            public string SourceId => string.Empty;

            public override string ToString() => Presentation;
        }

        public class SFSolutionChecker : ISolutionChecker
        {
            public Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response)
            {
                var typed = stimulus as SFStimulus;
                if (typed == null)
                    throw new NotImplementedException();
                var simplified = SimplifyFractionsGenerator.Simplify(typed);

                // TODO: if not correct, try simplifying response and if so give info that it can be simplified further (and if it would be correct or not)

                var correct = Clean(simplified.Presentation) == Clean(response.ResponseText);
                return Task.FromResult((ISolutionAnalysis)new SimpleSolutionAnalysis {
                    IsCorrect = simplified.Denominator == 1
                        ? correct || Clean(response.ResponseText) == $"{simplified.Numerator}"
                        : correct,
                });

                string Clean(string s) => s.Replace(" ", "");
            }

            public IUserResponse Deserialize(object obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
