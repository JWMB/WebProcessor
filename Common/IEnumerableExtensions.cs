using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Common
{
    public static class IEnumerableExtensions
    {
        // TODO: how to make generic for types that have + implemented?
        public static decimal SumOrDefault(this IEnumerable<decimal> values, decimal defaultValue = 0)
            => values.Any() ? values.Sum() : defaultValue;
        public static int SumOrDefault(this IEnumerable<int> values, int defaultValue = 0)
            => values.Any() ? values.Sum() : defaultValue;
        public static long SumOrDefault(this IEnumerable<long> values, long defaultValue = 0)
            => values.Any() ? values.Sum() : defaultValue;

        public static decimal SumOrDefault<T>(this IEnumerable<T> values, Func<T, decimal> selector, decimal defaultValue = 0) where T : notnull
            => values.Any() ? values.Sum(selector) : defaultValue;
        public static int SumOrDefault<T>(this IEnumerable<T> values, Func<T, int> selector, int defaultValue = 0) where T : notnull
            => values.Any() ? values.Sum(selector) : defaultValue;
        public static long SumOrDefault<T>(this IEnumerable<T> values, Func<T, long> selector, long defaultValue = 0) where T : notnull
            => values.Any() ? values.Sum(selector) : defaultValue;


        public static DateTime MinOrDefault<T>(this IEnumerable<T> values, Func<T, DateTime> selector, DateTime defaultValue)
    => values.Any() ? values.Min(selector) : defaultValue;

		public static IEnumerable<(T, T)> Pairs<T>(this IEnumerable<T> items)
		{
			var e = items.GetEnumerator();
			if (e.MoveNext())
			{
				var last = e.Current;
				while (e.MoveNext())
				{
					yield return (last, e.Current);
					last = e.Current;
				}
			}
		}

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

	public static class StringExtensions
	{
		public static IEnumerable<string> SplitYield(this string str, params char[] splitBy)
		{
			var index = 0;
			var len = str.Length;
			while (index < len)
			{
				var next = str.IndexOfAny(splitBy, index);
				if (next == -1)
				{
					yield return str.Substring(index);
					break;
				}
				yield return str[index..next];
				index = next + 1;
			}
		}

		public static IEnumerable<List<string>> ReadAsCsv(this string data)
		{
			var rx = new Regex(@"(?<=^|,)((""[^""]*"")|([^,]*))(?=$|,)");
			Func<string, List<string>> parseRow = (str) => rx.Matches(str).OfType<Match>().Select(o => o.Value).ToList();

			foreach (var (index, row) in data.SplitYield('\n').Select(o => o.Trim('\r')).Index()) //.Where(o => o.Any()) Select(o => o.Trim())
			{
				//var cells = rx.Matches(row).OfType<Match>().ToList();
				if (index == 0)
				{
					var cells = parseRow(row);
					if (cells.Count == 1)
					{
						cells = row.Split('\t').ToList();
						if (cells.Count > 1)
						{
							parseRow = str => str.Split('\t').ToList();
						}
					}
					//var rxTab = new Regex(@"([^\t]+)\t?|\t");
					//cells = rxTab.Matches(row).OfType<Match>().ToList();
					//if (cells.Count > 1)
					//	rx = rxTab;
				}
				yield return parseRow(row); //.Select(o => o.Groups[1].Value).ToList();
			}
		}

		public static string ToFirstLower(this string str) => str.Any() == false ? str : $"{str.Substring(0, 1).ToLower()}{str.Substring(1)}";

		public static string? IsNullOrEmpty(this string? str, string? fallback) => string.IsNullOrEmpty(str) ? fallback : str;

		public static string ReplaceRx(this string value, [StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, string replacement) => new Regex(regexPattern).Replace(value, replacement);

		public static string ReplaceRx(this string value, [StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, MatchEvaluator evaluator) => new Regex(regexPattern).Replace(value, evaluator);

		public static string ReplaceRange(this string value, int start, int end, string replacement)
		{
			return $"{value[0..start]}{replacement}{value[end..]}";
		}

		public static string Ellipse(this string value, int maxLength, string ellipse = "")
		{
			if (value.Length <= maxLength)
				return value;
			return $"{value[0..(maxLength - ellipse.Length)]}{ellipse}";
		}

		public static string ThrowIfNullOrEmpty([NotNull] this string? value, string? message = null)
		{
			if (value?.Any() != true) throw new ArgumentNullException(message);
			return value;
		}

		public static string DefaultIfNullOrEmpty(this string? value, string fallback) =>
			value?.Any() != true ? fallback : value;
		//public static string DefaultIfNullOrEmptyString(this string? value, string fallback) =>
		//	value?.Any() != true ? fallback : value;
	}

}
