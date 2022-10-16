using ProblemSource.Models;

namespace ProblemSource.Tests
{
    public static class LogItemsExtensions
    {
        public static IEnumerable<LogItem> Prepare(this IEnumerable<LogItem> items)
        {
            foreach (var item in items)
            {
                item.className = item.GetType().Name;
            }
            return items;
        }
    }
}
