namespace ProblemSourceModule.Services.ProblemGenerators
{
    public interface INumberGenerator
    {
        IEnumerable<decimal> GetAll();
        int? Count { get; }
        public decimal Get(int index)
        {
            var cnt = Count;
            if (cnt.HasValue)
               index = index % cnt.Value;
            return GetAll().Skip(index).First();
        }
    }

    public readonly record struct PrimeGenerator : INumberGenerator
    {
        public int? Count => null;

        public IEnumerable<decimal> GetAll() => Generate().Select(o => (decimal)o);

        public static IEnumerable<int> Generate()
        {
            // https://eli.thegreenplace.net/2023/my-favorite-prime-number-generator/
            yield return 2;
            var D = new Dictionary<int, int>();
            var q = 3;
            while (true)
            {
                if (D.TryGetValue(q, out var p) == false)
                {
                    D[q * q] = q;
                    yield return q;
                }
                else
                {
                    var p2 = p + p;
                    var x = q + p2;
                    foreach (var v in D.Values)
                        if (x == v)
                            x += p2;
                    D[x] = p;
                }
                q += 2;
            }
        }

        public static List<int> GetPrimeConstituents(int value)
        {
            var result = new List<int>();
            var maxPrime = (int)Math.Sqrt(value);
            var primes = Generate().GetEnumerator();
            while (true)
            {
                primes.MoveNext();
                var prime = primes.Current;
                if (prime > maxPrime)
                    break;
                while (value % prime == 0)
                {
                    result.Add(prime);
                    value /= prime;
                }
            }
            return result;
        }
    }

    public readonly record struct NumberRange(decimal Min, decimal Max, decimal Step = 1, bool Inclusive = true) : INumberGenerator
    {
        public int? Count => GetAll().Count();

        public IEnumerable<decimal> GetAll()
        {
            var min = Min;
            var step = Step;
            return Enumerable.Range(0, (int)((Max - Min) * 1M / Step)).Select((o, i) => min + i * step);
        }
    }

}
