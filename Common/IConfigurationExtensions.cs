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

        public static string SectionToJson(this IConfigurationSection section)
        {
            var entries = new Dictionary<string, string>();

            var isList = section.GetChildren().All(o => int.TryParse(o.Key, out var _));

            foreach (var child in section.GetChildren())
            {
                var value = child.Value != null
                    ? $"\"{child.Value}\""
                    : SectionToJson(child);

                entries.Add(child.Key, value);
            }

            return isList
                ? $"[{string.Join(",", entries.OrderBy(o => int.Parse(o.Key)).Select(o => o.Value))}]"
                : $"{{{string.Join(",", entries.Select(o => $"\"{o.Key}\": {o.Value}"))}}}";
        }
    }
}
