using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoK.Models.Raw;
using System.Text.RegularExpressions;

namespace NoK.Models
{
    public class Subpart
    {
        public string Title { get; set; } = string.Empty;
        public int LessonId { get; set; }
        public List<IAssignment> Assignments { get; set; } = new();
    }

    public enum ResponseType
    {
        None,
        Multiple,
        Absolute,
        Checkbox,
        Show,
        Unknown
    }

    public interface IAssignment
    {
        int Id { get; }
        string Body { get; }
        string? Suggestion { get; }
        Dictionary<string, string> Settings { get; }
        ResponseType ResponseType { get; }
        List<Subtask> Tasks { get; }

        public Url? Illustration { get; }
    }

    public abstract class AssignmentBase : IAssignment
    {
        public int Id { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
        public Dictionary<string, string> Settings { get; set; } = new();
        public ResponseType ResponseType { get; set; }

        public abstract List<Subtask> Tasks { get; }

        public Url? Illustration { get; set; }
    }

    public class AssignmentMultiChoice : AssignmentBase
    {
        public List<string> Alternatives { get; set; } = new();
        public Subtask Task { get; set; }
        public override List<Subtask> Tasks => new List<Subtask>{ Task };
    }

    public class Assignment : AssignmentBase
    {
        private List<Subtask> tasks = new();
        public override List<Subtask> Tasks => tasks;
        public string? Unit { get; set; }


        public static IAssignment FromRaw(string json)
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

        public static IAssignment Create(RawAssignment.Assignment src)
        {
            AssignmentBase result;
            if (src.TemplateData.Respons?.Any() == true)
            {
                var multiChoice = new AssignmentMultiChoice();
                result = multiChoice;
                multiChoice.Alternatives = src.TemplateData.Respons.Select(o =>
                {
                    var val = o switch
                    {
                        JProperty jp => jp.Value<string>() ?? "",
                        JObject jo => jo.Value<string>("value") ?? "",
                        string str => str,
                        _ => throw new Exception($"Unknown 'Respons': {o}")
                    };
                    return Process(val) ?? "";
                    //var add = switch result.ResponseType {
                    //     ResponseType.Multiple => "<input type=\"checkbox\"/>",
                    //    ResponseType.Checkbox => "<input type=\"checkbox\"/>",
                    //_ => ""
                    //}
                    //return $"{Process(val)}{add}";
                }).ToList();
                // [fraction before="a)" after="=5" num="" den="-3"]
                if (!string.IsNullOrEmpty(src.TemplateData.Unit))
                    throw new Exception($"Unit in {multiChoice.GetType().Name}");
            }
            else
            {
                var regular = new Assignment();
                regular.Unit = src.TemplateData.Unit;
                result = regular;
            }

            if (!string.IsNullOrEmpty(src.TemplateData.Illustration))
                result.Illustration = new Url(src.TemplateData.Illustration);

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
                .Select(o => new Subtask { Question = Process(o.q ?? o.v) ?? "", AnswerTypeString = o.u })
                .ToList();
            if (!tasks.Any())
                tasks.Add(new Subtask { Question = "", AnswerTypeString = null });

            foreach (var task in tasks)
                task.Parent = result;

            if (result is AssignmentMultiChoice mc)
                mc.Task = tasks.Single();
            else
                ((Assignment)result).tasks = tasks;

            foreach (var sol in src.Solutions)
            {
                var found = FindSubtask(sol.Subtask);
                if (found == null)
                    continue;
                found.Solution = ParseSolutions(sol.Solutions) ?? new();
                var answers = JArray.Parse(sol.Answers);
                found.Answer = answers.Select(item =>
                {
                    if (item["text"] == null)
                        throw new Exception($"No text node in {result.Id}/{sol.Id}");
                    var fragments = ParseFragment($"{item["text"]}".Replace("<br>", " "));
                    return string.Join("\n", fragments.Select(o => o.TextContent.Replace("\n", " ").Trim()).Where(o => o.Any()));
                }).OfType<string>().ToList();
                //found.Answer = JArray.Parse(sol.Answers)?.Select(o => Process(o["text"]?.Value<string>())).OfType<string>().ToList();
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

            tasks.RemoveAll(o => !o.Question.Any() && !o.Solution.Any() && !o.Answer.Any());

            result.Body = src.TemplateData.Text;
            result.Suggestion = src.TemplateData.Suggestion;
            result.ResponseType = Enum.TryParse<ResponseType>(src.TemplateData.ResponseType ?? src.TemplateData.ResponsType, true, out var r) ? r : ResponseType.Unknown;
            result.Id = src.AssignmentID ?? src.AssignmentId ?? 0;

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
                .Select(o => o.Trim())
                .Where(o => o.Any())
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

    //public class Maintask
    //{
    //    public List<Subtask> Subtasks { get; set; } = new();
    //}

    public enum AnswerType
    {
        Undefined,
        YesNo
    }

    public class Subtask
    {
        public IAssignment? Parent { get; set; }
        public List<string> Hint { get; set; } = new();
        public List<string> Answer { get; set; } = new();
        public string Question { get; set; } = string.Empty;
        public AnswerType AnswerType { get; set; }
        public List<string> Solution { get; set; } = new();

        public string? AnswerTypeString
        {
            set
            {
                AnswerType = value switch
                {
                    "Ja/Nej" => AnswerType.YesNo,
                    null => AnswerType.Undefined,
                    "" => AnswerType.Undefined,
                    _ => throw new NotImplementedException(value)
                };
            }
        }

        public override string ToString()
        {
            return $"({(Hint?.Any() != true? "" : "H")}{(Solution?.Any() != true ? "" : "S")}{(Answer?.Any() != true ? "" : "A")}){Question}:{AnswerType}";
        }
    }
}