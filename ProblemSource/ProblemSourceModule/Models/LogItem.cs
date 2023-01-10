using Common;
using Newtonsoft.Json;

namespace ProblemSource.Models
{
    public class LogItem
    {
        public string className { get; set; } = "";
        public long time { get; set; }
        public string type { get; set; } = "";

        public static string? GetEventClassName(object item)
        {
            // what the hell? Suddenly the deserialized items are JsonElement instead of dynamic object
            if (item is System.Text.Json.JsonElement jEl)
                return jEl.GetProperty("className").GetString();
            else
                return ExceptionTools.TryOrDefault(() => (string)((dynamic)item).className, null);
        }

        private static Dictionary<string, Type>? nameToType;

        public static (LogItem? parsed, Exception? error) TryDeserialize(object item)
        {
            if (nameToType == null)
            {
                nameToType = typeof(LogItem).Assembly.GetTypes().Where(o => typeof(LogItem).IsAssignableFrom(o)).ToDictionary(o => o.Name, o => o);
            }
            // TODO: custom serializer

            var className = GetEventClassName(item);
            if (className == null)
                return (null, new Exception("Couldn't get className"));

            if (!nameToType.TryGetValue(className, out var type))
                return (null, new Exception($"Non-existing type: '{className}'"));

            try
            {
                var asJson = item is System.Text.Json.JsonElement jEl ? jEl.ToString() : JsonConvert.SerializeObject(item);
                var typed = JsonConvert.DeserializeObject(asJson!, type);
                if (typed == null)
                    return (null, new Exception($"Could not deserialize"));
                if (typed is not LogItem li)
                    return (null, new Exception($"Not a {nameof(LogItem)}: '{typed.GetType().Name}'"));
                return (li, null);
            }
            catch (Exception ex)
            {
                //unhandledItems.Add(new { Exception = ex, Item = item });
                return (null, ex);
            }
        }
    }
}
