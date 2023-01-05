using System;
namespace Common
{
    public static class ExceptionExtensions
    {
        public static bool IsOrContains<T>(this Exception ex, out T? found) where T : Exception
        {
            found = null;
            if (ex is T f)
                found = f;

            if (ex is AggregateException aEx)
                found = aEx.InnerExceptions.OfType<T>().FirstOrDefault();

            return found != null;
        }
    }
}
