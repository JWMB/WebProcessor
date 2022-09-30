using System.Runtime.CompilerServices;

namespace Common
{
    public static class IEnumerableExtensions
    {
        public static decimal SumOrDefault(this IEnumerable<decimal> values, decimal defaultValue)
            => values.Any() ? values.Sum() : defaultValue;

        public static decimal SumOrDefault<T>(this IEnumerable<T> values, Func<T, decimal> selector, decimal defaultValue)
where T : notnull
    => values.Any() ? values.Sum(selector) : defaultValue;

    }
}
