using System.Text.Json;
using System.Text.Json.Nodes;

namespace Common
{
    public readonly record struct AzureConfigEntry(string Name, string Value, bool SlotSetting)
    {
        public static string ToAzureJson(IEnumerable<AzureConfigEntry> entries)
        {
            return JsonSerializer.Serialize(entries, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        }

        public static List<AzureConfigEntry> FromJsonConfigString(string json)
        {
            var parsed = JsonObject.Parse(json);
            if (parsed is JsonObject jobj == false)
                throw new Exception("Could't parse json");
            return FromJsonObject(jobj);
        }

        public static List<AzureConfigEntry> FromJsonObject(JsonObject root)
        {
            var entries = new List<AzureConfigEntry>();
            var path = new Stack<string>();
            Rec(root);

            void AddEntry(JsonValue value, bool isSlotSetting = false)
            {
                entries.Add(new AzureConfigEntry(string.Join(":", path.Reverse()), $"{value}", isSlotSetting));
            }

            return entries!; // JsonSerializer.Serialize(entries);

            void Rec(JsonNode node)
            {
                if (node is JsonObject jobj)
                {
                    foreach (var item in jobj)
                    {
                        path.Push(item.Key);
                        if (item.Value is JsonObject jobjChild)
                        {
                            Rec(jobjChild);
                        }
                        else if (item.Value is JsonArray jarr)
                        {
                            if (jarr.Any())
                            {
                                var index = 0;
                                foreach (var x in jarr)
                                {
                                    path.Push($"{index}");
                                    Rec(x);
                                    path.Pop();
                                }
                            }
                        }
                        else
                        {
                            if (item is KeyValuePair<string, JsonNode?> joo)
                            {
                                if (joo.Value != null)
                                {
                                    Rec(joo.Value);
                                }
                            }
                            else
                            {
                                throw new NotImplementedException($"{item}");
                            }
                        }
                        path.Pop();
                    }
                }
                else
                {
                    var val = node.AsValue();
                    if (val != null)
                    {
                        AddEntry(val);
                    }
                    else
                    {
                        throw new NotImplementedException($"{node}");
                    }
                }
            }
        }
    }
}
