using Common;
using Scriban;
using Scriban.Runtime;
using System.Text.RegularExpressions;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
	public class Replacer
	{
		public static Dictionary<string, object?> ToDictionary(object? obj)
		{
			if (obj is Dictionary<string, object?> d)
				return d;
			var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
			if (obj != null)
				foreach (var p in obj.GetType().GetProperties())
					dict[p.Name] = p.GetValue(obj);
			return dict;
		}

		private string PreProcess(string input, object replacements, IEnumerable<(string, Delegate)>? functions = null)
		{
			var matches = Regex.Matches(input, @"\{\{\s*(?<isEnd>end )?pre\s*\}\}").OfType<Match>().ToList();
			if (matches.Count % 2 != 0)
				throw new Exception($"Preprocessor Mismatching pairs ({matches.Count})");

			var modifications = matches
				.Pairs()
				.Select(pair =>
				{
					var (a, b) = pair;
					if (a.Groups["isEnd"].Success == true || b.Groups["isEnd"].Success == false)
						throw new Exception("Preprocessor mismatch pre / end pre");
					return new { Start = a.Index, End = b.Index + b.Length, Inner = input[(a.Index + a.Length)..(b.Index)] };
				});

			foreach (var mod in modifications.Reverse())
			{
				var inner = Execute(mod.Inner, replacements);
				input = input.ReplaceRange(mod.Start, mod.End, inner);
			}

			return input;
		}

		public string Execute(string input, object replacements, Dictionary<string, string>? templates = null, IEnumerable<(string, Delegate)>? functions = null,
			MemberRenamerDelegate? memberRenamer = null)
		{
			if (templates != null)
			{
				// TODO: implement Scriban recursive template inclusion
				var templateMatches = Regex.Matches(input, @"\{\{template:([^}]+)\}\}");
				foreach (Match match in templateMatches)
				{
					var templateName = match.Groups[1].Value.Trim();
					if (templates.TryGetValue(templateName, out var templateContent))
					{
						var replacedTemplateContent = Execute(templateContent, replacements);
						input = input.Replace(match.Value, replacedTemplateContent);
					}
				}
			}

			input = PreProcess(input, replacements);

			// adapt escape sequence to scriban
			input = Regex.Replace(input, @"\$\{([^}]+)\}", "{{$1}}");
			var parsedTemplate = Template.Parse(input);

			var context = CreateContext(replacements);

			if (functions != null)
			{
				var scriptObject = new ScriptObject();
				foreach (var (name, func) in functions)
					scriptObject.Import(name, func);
				context.PushGlobal(scriptObject);
			}
			//AddFunctions(context);

			// null memberRenamer doesn't work!? "Stack is empty"?!
			var result = memberRenamer == null ? parsedTemplate.Render(context) : parsedTemplate.Render(context, memberRenamer);
			return result;
		}

		public static void AddFunctions(TemplateContext context)
		{
			var scriptObject = new ScriptObject();
			scriptObject.Import("files", (IEnumerable<string> files) => $"{string.Join("\n", files.Select(o => $"{{{{ file:\"{o}\" }}}}"))}");
			context.PushGlobal(scriptObject);
		}

		public static TemplateContext CreateContext(object replacements)
		{
			var obj = new ScriptObject(StringComparer.OrdinalIgnoreCase);
			if (replacements is System.Collections.IDictionary dict)
				foreach (var k in dict.Keys)
					obj.Add($"{k}", dict[k]);
			else
				foreach (var p in replacements.GetType().GetProperties())
					obj.Add(p.Name, p.GetValue(replacements));
			var context = new TemplateContext();
			context.PushGlobal(obj);

			return context;
		}
	}
}