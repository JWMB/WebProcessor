using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
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

    public class ContentNode
    {
        public ContentNode() { }
        public ContentNode(int id)
        {
            Id = id;
        }
        public ContentNode? Parent { get; private set; }
        public IEnumerable<ContentNode> Children() => _children;
        private List<ContentNode> _children = new();
        public int Id { get; protected set; }
        public string Name { get; protected set; } = string.Empty;

        public virtual string? OtherContent => null;

        public void AddChild(ContentNode child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public IEnumerable<ContentNode> Descendants()
        {
            foreach (var child in _children)
            {
                yield return child;
                foreach (var grandchild in child.Descendants())
                    yield return grandchild;
            }
        }

        public IEnumerable<ContentNode> Ancestors()
        {
            if (Parent != null)
            {
                yield return Parent;
                foreach (var item in Parent.Ancestors())
                    yield return item;
            }
        }
        public override string ToString() => $"{GetType().Name}:{Id}";
    }

    public class ProductNode : ContentNode
    {
        public string ProductInfo { get; private set; } = string.Empty;
        public override string? OtherContent => ProductInfo;

        public static ProductNode Create(RawCourse.Root raw)
        {
            var isbn = Regex.Match($"{raw.ProductInfo}", @"ISBN:\D*(?<id>\d{11,14})");
            var id = int.Parse(isbn.Success ? isbn.Groups["id"].Value.Substring(0, 9) : "123456789");
            var node = new ProductNode { ProductInfo = raw.ProductInfo, Id = id };
            foreach (var child in raw.Content.Chapters.Select(ChapterNode.Create))
                node.AddChild(child);

            return node;
        }
    }

    public class ChapterNode : ContentNode
    {
        public static ChapterNode Create(RawCourse.Chapter raw)
        {
            var node = new ChapterNode { };
            if (raw.HierarchyID == null)
                throw new ArgumentNullException(nameof(raw.HierarchyID));
            node.Id = raw.HierarchyID.Value;
            node.Name = raw.Name;

            foreach (var child in raw.Parts.SelectMany(o => o.SubParts).Select(SubpartNode.Create))
                node.AddChild(child);
            return node;
        }
    }
    public class SubpartNode : ContentNode
    {
        public static SubpartNode Create(RawCourse.SubPart raw)
        {
            var node = new SubpartNode { };
            if (raw.HierarchyID == null)
                throw new ArgumentNullException(nameof(raw.HierarchyID));
            node.Id = raw.HierarchyID.Value;
            node.Name = raw.Name;

            //foreach (var child in raw.Sections.Select(o => new { Section = o, o.Name, o.Lesson }).Select(o => LessonNode.Create(o.Lesson, o.Section.SectionAssignmentRelations)))
            foreach (var child in raw.Sections.Select(SectionNode.Create))
                node.AddChild(child);
            return node;
        }
    }

    public class SectionNode : ContentNode
    {
        public List<int> AssignmentIds { get; private set; } = new();
        public LessonNode? Lesson => Children().OfType<LessonNode>().FirstOrDefault();

        public static SectionNode Create(RawCourse.Section raw)
        {
            var node = new SectionNode { };
            if (raw.Id == null)
                throw new ArgumentNullException(nameof(raw.Id));
            node.Id = raw.Id.Value;
            node.Name = raw.Name;

            if (raw.SectionAssignmentRelations?.Any() == true)
                node.AssignmentIds = raw.SectionAssignmentRelations.Select(o => o.AssignmentId).OfType<int>().ToList();

            node.AddChild(LessonNode.Create(raw.Lesson));

            return node;
        }
    }

    public class LessonNode : ContentNode
    {
        public string Html { get; private set; } = string.Empty;
        public override string? OtherContent => Html;

        public static LessonNode Create(RawCourse.Lesson raw)
        {
            var result = new LessonNode();
            if (raw.Id == null)
                throw new ArgumentNullException(nameof(raw.Id));
            result.Id = raw.Id.Value;

            result.Html = Assignment.ReplaceAsciiMathWithMathML(raw.Html);
            //result.Html = raw.Html;

            return result;
        }
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
        public override List<Subtask> Tasks => new List<Subtask> { Task };
    }

    public class Assignment : AssignmentBase
    {
        private List<Subtask> tasks = new();
        public override List<Subtask> Tasks => tasks;
        public string? Unit { get; set; }

        public bool ShowAllSubtasks { get; set; }

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
			var tmpDbgRrsp = src.TemplateData.ResponseType ?? src.TemplateData.ResponsType;

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
            {
                var proposedTasks = new List<Subtask>();
                if (src.Solutions.Any() || src.Hints.Any())
                {
                    var cnt = Math.Min(src.Solutions.Count(), src.Hints.Count());
                    if (src.Solutions.Count() != src.Hints.Count())
                    {
						//throw new Exception($"Solutions {src.Solutions.Count()} / Hints {src.Hints.Count()} count not matching");
					}
					// <p><i>Lös uppgiften utan digitalt verktyg.</i></p><p>Uttrycket `(30 - a)//(2 + 4)` har värdet 3.</p><p>Vilket blir värdet om</p>
                    // <p>a) parentesen runt täljaren tas bort [input]</p><p>b) parentesen runt nämnaren tas bort [input]</p><p>c) båda parenteserna tas bort? [input]</p>
					proposedTasks.AddRange(Enumerable.Range(0, cnt).Select(o => new Subtask { Question = "", AnswerTypeString = null }));
                    //var sols = ParseSolutions(src.Solutions.First().Solutions);
                }
                if (proposedTasks.Any() == false)
					proposedTasks.Add(new Subtask { Question = "", AnswerTypeString = null });
                tasks.AddRange(proposedTasks);
			}

			tasks.Select((o, i) => new { Index = i, Obj = o })
                .ToList()
                .ForEach(o => {
                    o.Obj.Index = o.Index;
                    //o.Obj.Parent = result;
                });

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
                    var fragments = INodeExtensions.ParseFragment($"{item["text"]}".Replace("<br>", " "));
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
                found.Hint = hints.Select(o => Process(o["text"]?.Value<string>(), true) ?? "").ToList();
            }

            tasks.RemoveAll(o => !o.Solution.Any() && !o.Answer.Any()); //!o.Question.Any() && 

			result.Body = ProcessBodyAndUpdateTasks(src.TemplateData.Text, tasks, (int index) => new Subtask { Index = index });


			result.Suggestion = Process(src.TemplateData.Suggestion) ?? "";
            result.ResponseType = Enum.TryParse<ResponseType>(src.TemplateData.ResponseType ?? src.TemplateData.ResponsType, true, out var r) ? r : ResponseType.Unknown;
            result.Id = src.AssignmentID ?? src.AssignmentId ?? 0;

            foreach (var task in tasks)
                task.Parent = result;

            { // MathML conversions
                result.Body = ReplaceAsciiMathWithMathML(result.Body);
                foreach (var task in tasks)
                {
                    task.Question = ReplaceAsciiMathWithMathML(task.Question);
                    task.Answer = task.Answer.Select(ReplaceAsciiMathWithMathML).ToList();
                    task.Hint = task.Hint.Select(ReplaceAsciiMathWithMathML).ToList();
                    task.Solution = task.Solution.Select(ReplaceAsciiMathWithMathML).ToList();
                }
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

        public static string ReplaceAsciiMathWithMathML(string text) => ReplaceAsciiMathWithMathML(text, true);

        public static string ReplaceAsciiMathWithMathML(string text, bool ignoreException)
        {
            var split = text.Split('`'); // assuming that ` is never used outside of AsciiMath notation...
            return string.Join("", split.Select((o, i) =>
            {
                if (i % 2 == 0) return o;
                try
                {
                    var mathML = AsciiMath.Parser.ToMathMl(o);
                    if (!mathML.Contains("<math>"))
                    { }
                    return mathML;
                }
                catch (Exception e)
                {
                    if (ignoreException)
                        return o;
                    throw new Exception(o, e);
                }

            }));

            var rx = new Regex(@"`(((a?sin|a?cos|a?tan)?[a-zA-Z]?[0-9,.=+*⋅\/^()\s\[\]\}\{…-])+[a-z]?)`"); //
            return rx.Replace(text, m =>
            {
                var inner = m.Groups[1].Value;
                if (inner.Trim().Any() == false)
                    return inner;
                try
                {
                    var mathML = AsciiMath.Parser.ToMathMl(inner);
                    if (!mathML.Contains("<math>"))
                    { }
                    return mathML;
                }
                catch (Exception e)
                {
                    //Console.WriteLine($"{m.Groups[1].Value} {e.GetType()}");
                    if (ignoreException)
                        return m.Value;
                    throw new Exception(inner, e);
                }
            });
        }

        private static string ProcessBodyAndUpdateTasks(string body, List<Subtask> tasks, Func<int, Subtask> createNewSubtask)
		{
            body = Process(body) ?? "";

            var nodes = INodeExtensions.ParseFragment(body);

            var abcs = nodes.SelectMany(o => o.DescendantsAndSelf())
                .Select(o => new { Node = o, Match = new Regex(@"^\s*(?<character>[a-f])\)\s?", RegexOptions.IgnoreCase).Match(o.Text()) })
                .Where(o => o.Match.Success)
                .Select(o => new {
                    Node = o.Node is IHtmlElement el ? el : o.Node.Ancestors().OfType<IHtmlElement>().First(),
					Char = o.Match.Groups["character"].Value.ToLower()[0]
				})
                .ToList();

			if (abcs.Count > 1)
            {
				var consecutive = new List<IHtmlElement>();
				var last = 'a' - 1;
				foreach (var item in abcs)
				{
					if (item.Char == last + 1)
					{
						consecutive.Add(item.Node);
						last = item.Char;
					}
				}
                if (consecutive.Count > 1)
                {
                    var orgTaskCount = tasks.Count;
                    consecutive
                        .Select((o, i) => new { Item = o, Index = i })
                        .ToList()
                        .ForEach(item =>
                        {
                            var existingTask = tasks.SingleOrDefault(o => o.Index == item.Index);
							//var newTaskNeeded = item.Index < tasks.Count;
							var task = existingTask ?? createNewSubtask(orgTaskCount + item.Index);
                            if (existingTask == null)
                                tasks.Add(task);
                            task.Question = item.Item.Html();
						});
					body = RemoveFromBodyAndRender(consecutive);
                    return body;
                }
			}

			if (body.Contains("<input"))
			{

				var inputs = nodes.SelectMany(o => o.DescendantsAndSelf<IHtmlInputElement>()).ToList();
				//var matches = new Regex(@"<input").Matches(result.Body);
				if (inputs.Any())
				{
                    // Example bad body: one <input> but multiple sections... Perhaps better to assume ABC form and try <input> as a fallback?
					// <p><i>Lös uppgiften utan digitalt verktyg.</i></p><p>a) Beräkna `2 * 5^2 - 5` <input type="text"/></p><p>b) Eric skriver på ett prov:<br>`2 * 5^2 - 5 = 5 * 5 = 25 * 2 = 50 - 5 = 45`<br>Svaret är rätt, men läraren ger ändå Eric fel. Varför?</p><p>c) Ge exempel på hur man kan skriva en korrekt beräkning.</p>
					var tmp = new Regex(@"(<br/?\s*>)[^<]*<input").Matches(body);
					if (tmp.Count < inputs.Count)
					{
						var groupedByParent = inputs.GroupBy(o => (o.Parent as IHtmlElement)?.OuterHtml).ToList();
						for (int i = tasks.Count; i < groupedByParent.Count; i++)
							tasks.Add(createNewSubtask(i));
						    groupedByParent.Select((o, i) => new { Html = o.Key, Index = i }).ToList() //o.First().Parent
							    .ForEach(o => tasks[o.Index].Question = o.Html ?? "");
                        body = RemoveFromBodyAndRender(inputs.Select(o => o.Parent).OfType<INode>());
					}
					else
					{
                        TryHandleSectionsABC(nodes);
					}
					//if (tasks.Count > 1)
					//{ }
					//else if (tasks.Count == 1 && tasks.Single().Question.Any())
					//{
					//}
				}
			}

            if (tasks.All(o => string.IsNullOrEmpty(o.Question)))
            {
				// <p><i>Lös uppgiften utan digitalt verktyg.</i></p><p>Vi antar att siffertangenten 4 är trasig på ditt digitala verktyg.&nbsp;<br>Hur räknar du då ut</p><p>a) `14*34`</p><p>b) `478*444`?</p>
				TryHandleSectionsABC(nodes);
			}

            return body;

            void TryHandleSectionsABC(List<INode> nodes)
            {
				if (body.Contains("a)"))
				{
					var alts = new Regex(@"<br/?>\s*\w\)\s+").Matches(body);
					if (alts.Count < 2)
					{
                        var tmp = new Regex(@">\s*([a-f])\)\s").Matches(body);
                        if (tmp.Count >= 2)
                        {
                            var consecutive = new List<Match>();
                            var last = 'a' - 1;
                            foreach (Match item in tmp)
                            {
                                var ch = item.Groups[1].Value[0];
								if (ch == last + 1)
                                {
									consecutive.Add(item);
									last = ch;
								}
                            }
                            if (consecutive.Count >= 2)
                            {
                                try
                                {
                                    for (int i = 0; i < consecutive.Count; i++)
                                    {
                                        var toIndex = i < consecutive.Count - 1 ? consecutive[i + 1].Index : body.Length - 1;
                                        var content = body.Substring(consecutive[i].Index, toIndex - consecutive[i].Index);
                                        tasks[i].Question = content;
                                    }
									body = body.Remove(consecutive.First().Index);
								}
								catch (Exception ex)
                                {
                                }
							}
						}
                    }
					else
					{
						var questions = ExtractEmbeddedQuestions(nodes);

						body = string.Join("\n", nodes.Select(o => o.ToHtml()));
						foreach (var item in questions)
							body = body.Replace((item as IHtmlElement)?.InnerHtml ?? "", "");

						for (int i = tasks.Count; i < questions.Count; i++)
							tasks.Add(createNewSubtask(i)); //

						questions.Select((o, i) => new { Node = o, Index = i }).ToList()
							.ForEach(o => tasks[o.Index].Question = o.Node.ToHtml() ?? ""); //(o.Parent as IHtmlElement)?.InnerHtml
					}
				}
				else
				{ }
			}

            string RemoveFromBodyAndRender(IEnumerable<INode> remove)
            {
				nodes.ForEach(o => o.RemoveDescendants(remove));
				var stripped = string.Join("\n", nodes.OfType<IHtmlElement>().Select(o => o.OuterHtml));
				return stripped;
			}
		}

		public static List<INode> ExtractEmbeddedQuestions(IEnumerable<INode> nodes)
        {
            var rx = new Regex(@"^\s*\w\)\s+");
            var candidates = nodes.SelectMany(o => o.Descendants().OfType<IText>().Where(p => rx.IsMatch(p.Text))).ToList();

            return candidates.Select((o, i) =>
            {
                var next = i < candidates.Count - 1 ? candidates[i + 1] : null;
                var copy = INodeExtensions.CreateCopyUntilMatch(o, node => node == next, true);
                return copy.Parent;
            })
                .Where(o => o is not null)
                .Select(o => o!)
                //.OfType<INode>()
                .ToList();
            /*<p>För vilka positiva heltalsvärden på `a` är kvoten `36`/`(a`/`10)`<br>a) mindre än 1<br>`a` <input type="text"><br>b) större än 9<br>`a` <input type="text"><br>c) mindre än 9<br>`a` <input type="text"><br>d) större än 3?<br>`a` <input type="text"></p>*/
        }

        private static string? RemoveRedundantWrapperElements(string? s)
        {
            if (s == null)
                return null;
            var modified = false;
            var fragments = INodeExtensions.ParseFragment(s).Where(o => o.TextContent.Trim().Any()).ToList();
            while (true)
            {
                // clean up lots of unnecessary <section>
                fragments = fragments.Where(o => o.TextContent.Trim().Any()).ToList();
                if (fragments.Count == 1 && fragments.Single().NodeName == "SECTION")
                {
                    fragments = fragments.Single().ChildNodes.ToList();
                    modified = true;
                    continue;
                }
                return modified
                    ? string.Join("\n", fragments.OfType<IElement>().Select(o => o.OuterHtml))
                    : s;
            }
        }

        private static string? Process(string? s, bool removeRedundantWrappers = false)
        {
            if (s == null)
                return null;

            if (removeRedundantWrappers)
                s = RemoveRedundantWrapperElements(s);

            return ContentTools.Process(
                s.ReplaceRx(@"\[lucktext[^\]]*\]", "<input type=\"text\"/>")
                    .ReplaceRx(@"(\<br\s*\/?\>\s*)+$", "")
                    .Replace("[input]", "<input type=\"text\"/>")
				);
        }

        public static List<string>? ParseSolutions(string data)
        {
            var sols = JArray.Parse(data);
            if (sols == null)
                return null;

            return sols
                .Select(o => Process(o["text"]?.Value<string>(), true) ?? "")
                .Select(o =>
                {
                    if (Regex.IsMatch(o, @"^\s*\<.+\>\s*$", RegexOptions.Multiline))
                    {
                        var wirises = INodeExtensions.ParseFragment(o).OfType<IElement>()
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
    }

    internal record Intermediate(string? q = null, string? u = null, string? v = null, string? h = null);

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
        public int Index { get; set; }

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

        public string Id => $"{Parent?.Id}/{Index}";

        public override string ToString()
        {
            return $"({(Hint?.Any() != true ? "" : "H")}{(Solution?.Any() != true ? "" : "S")}{(Answer?.Any() != true ? "" : "A")}){Question}:{AnswerType}";
        }

        public bool? CheckResponseIsCorrect(string response)
        {
            var rx = new Regex(@"^\s?(\d[., ]?)+$");
            var numericalAnswers = Answer.Select(Parse);
            var first = numericalAnswers.FirstOrDefault();
            if (first != null)
            {
                var responseDec = Parse(response);
				if (responseDec == null)
					throw new Exception("Could not parse user response");
                return responseDec == first;
            }

			return null;

            decimal? Parse(string input)
            {
                if (!rx.IsMatch(input))
                    return null;
                return decimal.TryParse(input.Replace(" ", ""), out var decimalValue) ? (decimal?)decimalValue : null;
			}
        }
    }
}