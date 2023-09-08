using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NumSharp.Utilities;

namespace Tools
{
    internal class ClientUtils
    {
        public static string CsvToNVRLevelStrings(string path)
        {
            var objs = CsvToJsonList(path);
            var isRP = path.ToLower().Contains("rp.xls");

            var index = 0;
            foreach (var obj in objs)
            {
                if (isRP)
                {
                    Stringify(obj, "distractorDimensions");
                    //  For some reason, these were not strings:
                    //Stringify(obj, "patternLengths");
                    //Stringify(obj, "randomPattern");

                    foreach (var name in obj.Properties().Where(o => o.Name.StartsWith("numSlicesPerColor") || o.Name.StartsWith("numColorsInShape")).Select(o => o.Name))
                        Stringify(obj, name);
                }
                else
                {
                    obj["rowNum"] = (index + 1);
                    //Remove(obj, "rowNum");
                }
                index++;
            }

            return $"[{string.Join(",\n", objs.Select(o => JsonConvert.SerializeObject(o, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })))}]";

            void Remove(JObject obj, string prop)
            {
                if (obj[prop] == null)
                    return;
                obj[prop]!.Remove();
            }

            void Stringify(JObject o, string prop)
            {
                var val = o[prop];
                if (val == null)
                    return;
                o[prop] = val.ToString();
            }
        }

        public static List<JObject> CsvToJsonList(string path)
        {
            var lines = File.ReadAllLines(path);

            var header = GetItems(lines.First());
            while (header.Last().Trim() == "")
                header = header.RemoveAt(header.Length - 1);
            //lowercase 1st
            header = header.Select(o => $"{o.First()}".ToLower() + o.Substring(1)).ToArray();

            var types = header.Select(o => (Type?)null).ToList();
            foreach (var line in lines.Skip(1).Take(100))
            {
                var items = GetItems(line);
                for (int i = 0; i < Math.Min(items.Length, header.Length); i++)
                {
                    if (items[i].Length > 0)
                    {
                        var isNumeric = decimal.TryParse(items[i], System.Globalization.CultureInfo.InvariantCulture, out var numeric);
                        var isDecimal = isNumeric && Math.Round(numeric) != numeric;
                        if (types[i] == null)
                        {
                            types[i] = isNumeric ? (isDecimal ? typeof(decimal) : typeof(int)) : typeof(string);
                        }
                        else
                        {
                            if ((types[i] == typeof(decimal) || types[i] == typeof(int)) && !isNumeric)
                                types[i] = typeof(string);
                            else if (types[i] == typeof(int) && isDecimal)
                                types[i] = typeof(decimal);
                        }
                    }
                }
            }

            var objs = new List<JObject>();
            foreach (var line in lines.Skip(1))
            {
                var obj = new JObject();
                var items = GetItems(line);
                for (int i = 0; i < Math.Min(items.Length, header.Length); i++)
                {
                    var item = items[i];
                    var type = types[i];
                    var val = type != null && type != typeof(string) && item.Any() ? Convert.ChangeType(item, type) : item;
                    if (val != null && (val is string s ? s != "": true))
                        obj.Add(header[i], new JValue(val));
                }
                objs.Add(obj);
            }

            return objs;

            string[] GetItems(string line) => line.Split('\t');
        }
    }
}
