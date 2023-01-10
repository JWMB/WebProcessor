namespace Common
{
    public static class IEnumerableExtensions
    {
        // TODO: how to make generic for types that have + implemented?
        public static decimal SumOrDefault(this IEnumerable<decimal> values, decimal defaultValue = 0)
            => values.Any() ? values.Sum() : defaultValue;
        public static int SumOrDefault(this IEnumerable<int> values, int defaultValue = 0)
            => values.Any() ? values.Sum() : defaultValue;

        public static decimal SumOrDefault<T>(this IEnumerable<T> values, Func<T, decimal> selector, decimal defaultValue = 0) where T : notnull
            => values.Any() ? values.Sum(selector) : defaultValue;
        public static int SumOrDefault<T>(this IEnumerable<T> values, Func<T, int> selector, int defaultValue = 0) where T : notnull
            => values.Any() ? values.Sum(selector) : defaultValue;


        public static DateTime MinOrDefault<T>(this IEnumerable<T> values, Func<T, DateTime> selector, DateTime defaultValue)
    => values.Any() ? values.Min(selector) : defaultValue;


        public static IEnumerable<IEnumerable<T>> SplitBy<T>(this IEnumerable<T> values, Func<T, bool> splitOn, bool splitItemAsLast = true)
        {
            var current = new List<T>();
            foreach (var item in values)
            {
                if (splitOn(item))
                {
                    if (splitItemAsLast)
                    {
                        current.Add(item);
                        yield return current;
                        current = new List<T>();
                    }
                    else
                    {
                        yield return current;
                        current = new List<T> { item };
                    }
                }
                else
                {
                    current.Add(item);
                }
            }
            yield return current;
        }
    }
}
