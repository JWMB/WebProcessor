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

    }
}
