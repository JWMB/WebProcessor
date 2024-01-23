using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoK.Models.Raw;
using System.Text.RegularExpressions;

namespace NoK.Tests
{
    public class Subpart
    {
        public string Title { get; set; } = string.Empty;
        public int LessonId { get; set; }
        public List<Assignment> Assignments { get; set; } = new();
    }

    public class Assignment
    {
        public List<Subtask> Tasks { get; set; } = new();
        public List<string> Alternatives { get; set; } = new();
        public string Body { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
        public string? ResponseType { get; set; }
        public string? Unit { get; set; }
        public Dictionary<string, string> Settings { get; set; } = new();
        public int Id { get; set; }


        public static Assignment FromRaw(string json)
        {
            var org = JsonConvert.DeserializeObject<RawAssignment.Assignment>(json);
            if (org == null)
                throw new Exception("Not deserializable");
            return Create(org);
        }

        public override string ToString()
        {
            return $"T:{Tasks.Count} {Body.Substring(0, 10)}...";
        }

        public static Assignment Create(RawAssignment.Assignment src)
        {
            var result = new Assignment();

            result.Body = src.TemplateData.Text;
            result.Suggestion = src.TemplateData.Suggestion;
            result.ResponseType = src.TemplateData.ResponseType ?? src.TemplateData.ResponsType;
            result.Id = src.AssignmentID ?? src.AssignmentId ?? 0;
            result.Unit = src.TemplateData.Unit;

            var intermediates = new List<Intermediate>();
            var jsonSettings = src.TemplateData.Settings as JObject;
            if (jsonSettings != null)
            {
                var normalized = new List<JObject>();
                var settings = new Dictionary<string, string>();
                if (jsonSettings is JObject jSettings)
                {
                    foreach (var prop in jSettings.Properties())
                    {
                        if (int.TryParse(prop.Name.Substring(1), out var index))
                        {
                            index--;
                            for (var i = normalized.Count; i <= index; i++)
                                normalized.Add(new());
                            normalized[index].Add(prop.Name.Remove(1), prop.Value);
                        }
                        else
                        {
                            settings.Add(prop.Name, prop.Value.ToString());
                        }
                    }
                }
                result.Settings = settings;

                intermediates = normalized.Select(o => o.ToObject<Intermediate>() ?? new()).ToList();
            }
            var tasks = intermediates
                .Where(o => o.q != null || o.v != null)
                .Select(o => new Subtask { Question = Process(o.q ?? o.v) ?? "", AnswerType = o.u ?? "" })
                .ToList();
            if (!tasks.Any())
                tasks.Add(new Subtask { Question = "", AnswerType = "" });
            result.Tasks = tasks;

            foreach (var sol in src.Solutions)
            {
                var found = FindSubtask(sol.Subtask);
                if (found == null)
                    continue;
                found.Solution = ParseSolutions(sol.Solutions) ?? new();
                found.Answer = JArray.Parse(sol.Answers)?.Select(o => Process(o["text"]?.Value<string>())).OfType<string>().ToList();
            }
            foreach (var hint in src.Hints)
            {
                var found = FindSubtask(hint.Subtask);
                if (found == null)
                    continue;
                var hints = JArray.Parse(hint.Hints);
                if (hints.Count > 1)
                { }
                found.Hint = hints.Select(o => Process(o["text"]?.Value<string>()) ?? "").ToList();
            }

            var responseType = src.TemplateData.ResponseType; // multiple absolute checkbox show
            if (src.TemplateData.Respons?.Any() == true)
            {
                var tmp = src.TemplateData.Respons.Select(o => o.Value is string str ? str : "");
                result.Alternatives = tmp
                  .Select(Process)
                  .Select(o => {
                      var add = "";
                      if (responseType == "multiple")
                          add = "<input type=\"checkbox\"/>";
                      else if (responseType == "checkbox")
                          add = "<input type=\"checkbox\"/>";
                      return $"{o}{add}";
                  }).ToList();
                // [fraction before="a)" after="=5" num="" den="-3"]
            }
            return result;

            Subtask? FindSubtask(string? id)
            {
                if (id?.Any() != true)
                    return tasks.FirstOrDefault(); // not sure if this is correct

                var found = tasks.FirstOrDefault(o => o.Question?.StartsWith(id) == true);
                if (found != null)
                    return found;

                var index = -65 + id.Substring(0, 1).ToUpper()[0];
                if (index >= 0 && index < tasks.Count)
                    return tasks[index];
                return null;
            }
        }

        private static string? Process(string? s)
        {
            return
                s == null
                ? null
                : ContentTools.Process(
                    s.ReplaceRx(@"\[lucktext[^\]]*\]", "<input type=\"text\"/>")
                       .ReplaceRx(@"(\<br\s*\/?\>\s*)+$", "")
                   );
        }

        public static List<string>? ParseSolutions(string data)
        {
            var sols = JArray.Parse(data);
            if (sols == null)
                return null;

            return sols
                .Select(o => Process(o["text"]?.Value<string>()) ?? "")
                .Select(o =>
                {
                    if (Regex.IsMatch(o, @"^\s*\<.+\>\s*$", RegexOptions.Multiline))
                    {
                        var wirises = ParseFragment(o).OfType<IElement>()
                            .SelectMany(o => o.GetElementsByClassName("Wirisformula").Select(ParseWiris))
                            .Where(o => o.Any());
                        if (wirises.Any())
                            return string.Join("\n", wirises);
                    }
                    return o;
                })
                .ToList();
        }

        public static string ParseWiris(IElement element)
        {
            return (element.GetAttribute("data-mathml") ?? "")
                               .ReplaceRx(@"«", "<")
                               .ReplaceRx(@"»", ">")
                               .ReplaceRx(@"¨", "\"")
                               .ReplaceRx(@"§(#\d+;)", "&$1");
        }

        static List<INode> ParseFragment(string? html)
        {
            if (html?.Any() == false)
                return new();
            var id = Guid.NewGuid().ToString();
            html = $"<div id={id}>{html}</div>";
            using var context = BrowsingContext.New(Configuration.Default);
            using var doc = context.OpenAsync(req => req.Content(html)).Result;
            var container = doc?.GetElementById(id);
            return container?.ChildNodes.ToList() ?? new();
        }
    }

    internal record Intermediate(string? q = null, string? u = null, string? v = null, string? h = null);

    public class Maintask
    {
        public List<Subtask> Subtasks { get; set; } = new();
    }

    public class Subtask
    {
        public List<string>? Hint { get; set; }
        public List<string>? Answer { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? AnswerType { get; set; }
        public List<string> Solution { get; set; } = new();

        public override string ToString()
        {
            return $"({(Hint == null ? "" : "H")}{(Solution == null ? "" : "S")}){Question}:{AnswerType}";
        }
    }
}