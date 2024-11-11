namespace ProblemSourceModule.Services.ProblemGenerators
{
    public class RandomCollectionTools
    {
        private readonly IRandomNumberGenerator rnd;

        public RandomCollectionTools(IRandomNumberGenerator rnd)
        {
            this.rnd = rnd;
        }

        public T GetWeightedRandom<T>(List<(double weight, T value)> weighted)
        {
            var total = 0.0;
            var sums = new List<double>();
            foreach (var kvp in weighted)
            {
                total += kvp.weight;
                sums.Add(total);
            }
            var r = rnd.NextDouble() * sums.Last();
            var index = sums.FindIndex(o => o > r);
            if (index < 0)
                index = 0;
            return weighted[index].value;
        }

        public IEnumerable<int> GetFrom(List<int> list, int count, bool unique = false)
        {
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

}
