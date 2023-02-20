using static ProblemSourceModule.Models.Aggregates.ML.MLFeaturesJulia;

namespace ProblemSourceModule.Models.Aggregates.ML
{
    public static class StatisticsExtensions
    {
        public static IEnumerable<TOut> AggregateWithPrevious<TIn, TOut>(this IEnumerable<TIn> values, Func<TIn, TIn, TOut> actOnPreviousAndCurrent)
        {
            var prev = values.First();
            foreach (var item in values.Skip(1))
            {
                yield return actOnPreviousAndCurrent(prev, item);
            }
        }

        public static object?[] ToObjectArray<T>(this IEnumerable<FeaturesForExercise> values, Func<FeaturesForExercise, T> selector) =>
            values.Select(selector).Select(o => (object?)o).ToArray();

        public static double? MedianOrNull(this IEnumerable<double> values) => values.Any() ? values.Median() : null;
        public static decimal? MedianOrNull(this IEnumerable<decimal> values) => values.Any() ? values.Median() : null;


        public static decimal Median(this IEnumerable<decimal> values) => (decimal)values.Order().Select(o => (double)o).Median();

        public static double Median(this IEnumerable<double> values)
        {
            if (values is not IOrderedEnumerable<double>)
                values = values.Order().ToArray();
            var enumerable = values as double[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count == 0)
                return 0;
            return enumerable[count / 2];
        }

        public static decimal StdDev(this IEnumerable<decimal> values) => (decimal)values.Select(o => (double)o).StdDev();

        public static double StdDev(this IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count <= 1)
                return 0;
            var avg = enumerable.Average();
            var sum = enumerable.Sum(d => (d - avg) * (d - avg));
            return Math.Sqrt(sum / (count - 1));
        }

        public static double? Skewness(this IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();

            if (values.Any() == false)
                return null;
            var avg = values.Average();
            var sd = values.StdDev();
            var cnt = (double)values.Count();

            var skewCum = 0.0d; // the cum part of SKEW formula
            for (int i = 0; i < enumerable.Length; i++)
            {
                var b = (enumerable[i] - avg) / sd;
                skewCum += b * b * b;
            }
            return cnt / (cnt - 1) / (cnt - 2) * skewCum;
        }
    }
}
