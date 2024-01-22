using Newtonsoft.Json.Linq;
using NoK.Models.Raw;
using System.Text.RegularExpressions;

namespace NoK.Tests
{
    public class Ass
    {
        public List<Subtask> Tasks { get; set; } = new();


        public override string ToString()
        {
            return $"{Tasks.Count}";
        }

        public static Ass Create(Assignment src)
        {
            var oxos = new List<Oxo>();
            var jsonSettings = src.TemplateData.Settings as JObject;
            if (jsonSettings != null)
            {
                var bloxo = new List<JObject>();
                if (jsonSettings is JObject jSettings)
                {
                    foreach (var prop in jSettings.Properties())
                    {
                        if (int.TryParse(prop.Name.Substring(1), out var index))
                        {
                            index--;
                            for (var i = bloxo.Count; i <= index; i++)
                                bloxo.Add(new());
                            bloxo[index].Add(prop.Name.Remove(1), prop.Value);
                        }
                        else
                        { }
                    }
                }
                oxos = bloxo.Select(o => o.ToObject<Oxo>() ?? new()).ToList();
            }
            var tasks = oxos
                .Where(o => o.q != null || o.v != null)
                .Select(o => new Subtask { Question = Process(o.q ?? o.v) ?? "", AnswerType = o.u ?? "" })
                .ToList();
            if (!tasks.Any())
                tasks.Add(new Subtask { Question = "", AnswerType = "" });

            foreach (var sol in src.Solutions)
            {
                var found = FindSubtask(sol.Subtask);
                if (found == null)
                    continue;
                var sols = JArray.Parse(sol.Solutions);
                if (sols != null)
                {
                    if (sols.Count == 1)
                        ;
                        //found.Solution = new Solution{  sols[0].ToString();
                    found.FullAnswer = sols
                        .Select(o => Process(o["text"]?.Value<string>()) ?? "")
                        .Select(o =>
                        {
                            if (Regex.IsMatch(o, @"^\s*\<.+\>\s*$"))
                            {

                            }
                            return o;
                        })
                        .ToList();
                }

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
                //found.Hints = found.Hint = .Select(o => Process(o["Text"]?.Value<string>()) ?? "")
            }


            return new Ass
            {
                Tasks = tasks
            };

            string? Process(string? s)
            {
                return
                    s == null
                    ? null
                    : ContentTools.Process(
                        s.ReplaceRx(@"\[lucktext[^\]]*\]", "<input type=\"text\"/>")
                           .ReplaceRx(@"(\<br\s*\/?\>\s*)+$", "")
                       );
            }

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
    }

    internal record Oxo(string? q = null, string? u = null, string? v = null, string? h = null);

    public class Maintask
    {
        public List<Subtask> Subtasks { get; set; } = new();
    }

    public class Subtask
    {
        public Hint? Hint { get; set; }
        public List<string>? Solution { get; set; }
        public List<string>? Answer { get; set; }
        public string Question { get; set; } = string.Empty;
        public string AnswerType { get; set; } = string.Empty;
        public List<string> FullAnswer { get; set; } = new();

        public override string ToString()
        {
            return $"({(Hint == null ? "" : "H")}{(Solution == null ? "" : "S")}){Question}:{AnswerType}";
        }
    }
}