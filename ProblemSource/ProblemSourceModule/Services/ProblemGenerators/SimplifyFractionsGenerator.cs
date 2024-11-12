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

        public static (decimal, decimal) Simplify(decimal numerator, decimal denominator)
        {
            var scale = Math.Max(numerator.Scale, denominator.Scale);
            var factToInts = (int)Math.Pow(10, scale);
            var primesNum = PrimeGenerator.GetPrimeConstituents((int)(numerator * factToInts));
            var primesDenom = PrimeGenerator.GetPrimeConstituents((int)(denominator * factToInts));

            var common = Supersect(primesNum, primesDenom).ToList();
            var divideBy = 1.0M / factToInts * common.Aggregate((p, c) => p * c);

            return ((int)(numerator / divideBy), (int)(denominator / divideBy));

            IEnumerable<int> Supersect(IEnumerable<int> a, IEnumerable<int> b)
            {
                ILookup<int, int> lookup2 = b.ToLookup(i => i);

                return
                (
                  from group1 in a.GroupBy(i => i)
                  let group2 = lookup2[group1.Key]
                  from i in (group1.Count() < group2.Count() ? group1 : group2)
                  select i
                ).ToArray();
            }
        }

        public class SFStimulus : IStimulus
        {
            public decimal Numerator { get; set; }
            public decimal Denominator { get; set; }
            public string Presentation => $"{Numerator} / {Denominator}";

            public string Id => Presentation;

            public string SourceId => string.Empty;

            public int Simplify()
            {
                var orgNumerator = Numerator;
                (Numerator, Denominator) = SimplifyFractionsGenerator.Simplify(Numerator, Denominator);
                return (int)(orgNumerator / Numerator);
            }
            public override string ToString() => Presentation;
            public override bool Equals(object? obj) => (obj as SFStimulus)?.Numerator == Numerator && (obj as SFStimulus)?.Denominator == Denominator;
        }

        public class SFUserResponse : SFStimulus, IUserResponse
        {
            public string ResponseText { get; set; } = string.Empty;
            public void Parse()
            {
                var split = ResponseText.Split('/').Select(o => o.Trim()).ToArray();
                Numerator = decimal.TryParse(split[0], out var n) ? n : 0;
                Denominator = split.Length == 1 ? 1 : decimal.TryParse(split[1], out var d) ? d : 0;
            }
        }

        public class SFSolutionChecker : ISolutionChecker
        {
            public Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response)
            {
                var typed = stimulus as SFStimulus;
                if (typed == null)
                    throw new NotImplementedException();
                typed.Simplify();

                if (response is not SFUserResponse typedResponse)
                    typedResponse = (SFUserResponse)Deserialize(response.ResponseText);

                var correct = typed.Equals(typedResponse);

                // if not correct, try simplifying response and if so give info that it can be simplified further (and if it would be correct or not)
                var hints = new List<string>();
                if (!correct)
                {
                    var factor = typedResponse.Simplify();
                    if (factor > 1)
                    {
                        hints.Add("Can be simplified further");
                        if (!typed.Equals(typedResponse))
                            hints.Add("Incorrect");
                    }
                }

                return Task.FromResult((ISolutionAnalysis)new SimpleSolutionAnalysis
                {
                    IsCorrect = correct,
                    Feedback = string.Join(", ", hints)
                });

            }

            public IUserResponse Deserialize(object obj)
            {
                var result = new SFUserResponse { ResponseText = obj?.ToString() ?? string.Empty };
                result.Parse();
                return result;
            }
        }
    }
}
