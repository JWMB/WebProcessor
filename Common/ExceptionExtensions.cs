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

    public static class ExceptionTools
    {
        public static T TryOrDefault<T>(Func<T> func, T defaultValue, Action<Exception>? onException = null)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                return defaultValue;
            }
        }
    }
}
