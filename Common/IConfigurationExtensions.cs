using Microsoft.Extensions.Configuration;

namespace Common
{
    public static class IConfigurationExtensions
    {
        public static T GetOrThrow<T>(this IConfiguration conf, string key)
        {
            var val = conf[key];
            if (val == null)
                throw new NullReferenceException(key);
            if (val is T s)
                return s;
            var typed = System.Text.Json.JsonSerializer.Deserialize<T>(val);
            if (typed == null)
                throw new NullReferenceException($"Couldn't convert {key} to {typeof(T).Name}");
            return typed;
        }
    }
}
