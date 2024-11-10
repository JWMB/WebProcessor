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

        public IEnumerable<decimal> GetAll()
        {
            var D = new Dictionary<int, int[]>();
            var q = 2;
            while (true)
            {
                var found = D.TryGetValue(q, out var f) ? f : null;
                if (found == null)
                {
                    D[q * q] = [q];
                    yield return q;
                }
                else
                {
                    foreach (var p in found)
                    {
                        var next = p + q;
                        if (D.TryGetValue(next, out var v))
                            v.Append(p);
                        else
                            D.Add(next, [p]);
                    }

                    D.Remove(q);
                }
                q += 1;
            }
        }

        public static List<int> GetPrimes(int value)
        {
            throw new NotImplementedException();
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
