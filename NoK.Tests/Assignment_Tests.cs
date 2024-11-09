using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoK.Models;
using NoK.Models.Raw;
using Shouldly;

namespace NoK.Tests
{
    public class Assignment_Tests
    {
        [Fact]
        public void CopyUntil()
        {
            var html =
"""
<p>Skriv sökta talet.</p>
<p>För vilka positiva heltalsvärden på `a` är kvoten `36`/`(a`/`10)`<br>a) mindre än 1<br>`a` <input
		type="text" /><br>b) större än 9<br>`a` <input type="text" /><br>c) mindre än 9<br>`a` <input
		type="text" /><br>d) större än 3?<br>`a` <input type="text" /></p>
""";
            var fragments = INodeExtensions.ParseFragment(html);
            var xx = fragments.SelectMany(o => o.DescendantsAndSelf<IText>()).First(o => o.Text.StartsWith("a)"));
            var tmp = INodeExtensions.CreateCopyUntilMatch(xx, node => node.TextContent.StartsWith("b)"), true);
            var aa = (tmp.Parent as IHtmlElement)?.InnerHtml;

            var questions = Assignment.ExtractEmbeddedQuestions(fragments);
        }

        [Fact]
        public void SubParts_Deserialize()
        {
            var subparts = LoadAllSubparts();
            var assignments = subparts.SelectMany(o => o.Assignments).ToList();

            var byUnit = assignments.OfType<Assignment>().GroupBy(o => o.Unit ?? "").ToDictionary(o => o.Key, o => o.ToList());
            var byType = assignments.OfType<Assignment>().GroupBy(o => o.ResponseType).ToDictionary(o => o.Key, o => o.ToList());
            //var strange = assignments.SelectMany(o => o.Tasks).Where(o => o.Hint?.Count > 1 || o.Solution?.Count > 1);
            //var questions = assignments.SelectMany(o => o.Tasks).Select(o => o.Question).ToList();

            var strange = assignments.Single(o => o.Id == 141109);

            var multiChoice = assignments.OfType<AssignmentMultiChoice>().ToList();
            multiChoice.Where(o => o.Alternatives.Any() == false).ShouldBeEmpty();
            multiChoice.Where(o => o.Tasks.Count() != 1).ShouldBeEmpty();

            multiChoice.Where(o => o.Task.Answer.Count() != 1).ShouldBeEmpty();
            var allMcAnswers = multiChoice.Select(o => o.Task.Answer.Single()).ToList();
            // hm, no computer-usable answers :(

            var regular = assignments.OfType<Assignment>().ToList();
            regular.Where(o => o.Tasks.Any(p => p.Answer.Count > 1)).ShouldBeEmpty();
            var withNoAnswers = regular.Where(o => o.Tasks.Any(p => p.Answer.Count == 0)).ToList();

            var withAnswers = regular.Except(withNoAnswers).ToList();

            var nonNumericAnswers = withAnswers.SelectMany(o => o.Tasks).Where(o => decimal.TryParse(o.Answer.Single(), out var _) == false).ToList();

            var withNumericAnswers = withAnswers.Except(nonNumericAnswers.Select(o => o.Parent)).ToList();
            var idsForWithNumericAnswers = withNumericAnswers.Select(o => o.Id).ToList();
        }

        [Fact]
        public void ParseSolution_Wirisformula()
        {
            var json = "[{\"text\":\"<div><img alt=\\\"fraction numerator negative 150 over denominator 3 end fraction space equals space minus 50\\\" class=\\\"Wirisformula\\\" data-mathml=\\\"\\u00abmath xmlns=\\u00a8http:\\/\\/www.w3.org\\/1998\\/Math\\/MathML\\u00a8\\u00bb\\u00abmfrac\\u00bb\\u00abmrow\\u00bb\\u00abmo\\u00bb-\\u00ab\\/mo\\u00bb\\u00abmn\\u00bb150\\u00ab\\/mn\\u00bb\\u00ab\\/mrow\\u00bb\\u00abmn\\u00bb3\\u00ab\\/mn\\u00bb\\u00ab\\/mfrac\\u00bb\\u00abmo\\u00bb\\u00a7#160;\\u00ab\\/mo\\u00bb\\u00abmo\\u00bb=\\u00ab\\/mo\\u00bb\\u00abmo\\u00bb\\u00a7#160;\\u00ab\\/mo\\u00bb\\u00abmo\\u00bb-\\u00ab\\/mo\\u00bb\\u00abmn\\u00bb50\\u00ab\\/mn\\u00bb\\u00ab\\/math\\u00bb\\\" src=\\\"\\/editor\\/ckeditor4\\/\\/plugins\\/ckeditor_wiris\\/integration\\/showimage.php?formula=4be1d4c6af8142695e03d3f5c7fd18fe\\\" \\/><\\/div>\\n\\n<div>&nbsp;<\\/div>\"}]";
            var tmp = Assignment.ParseSolutions(json);
            tmp!.Single().ShouldStartWith("<math xmlns");
        }

        [Theory]
        [InlineData(147964, 3)]
        [InlineData(147963, 2)]
        [InlineData(141107, 3)]
        [InlineData(141109, 4)]
        public void Investigate_InlineTasks(int assignmentId, int expectedNumTasks)
        {
            var rawAssignment = GetRawAssignment(assignmentId);

            var converted = Assignment.Create(rawAssignment!);
            converted.Tasks.Where(o => string.IsNullOrEmpty(o.Question)).ShouldBeEmpty();
            converted.Tasks.Count.ShouldBe(expectedNumTasks);
        }

        [Fact]
        public void Investigate_StrangeAssignment()
        {
            var raw = GetRawAssignment(24515, filename: "assignment2.json"); //24531
            var converted = Assignment.Create(raw!); // `20/"-5"` =
        }

        [Fact]
        public void Investigate_MathML()
        {
            var assignmentId = 141091;
            var rawAssignment = GetRawAssignment(assignmentId);
            //var tmp = Assignment.ReplaceAsciiMathWithMathML(rawAssignment.TemplateData.Text);

            var converted = Assignment.Create(rawAssignment!);
            converted.Body.ShouldContain("<math>");
        }

        //[Fact]
        //public void AsciiMath_Parser_Test()
        //{
        //    GetRawAssignments()
        //}

        [Theory]
        [InlineData("s=v*t")]
        [InlineData("s = v * t")]
        [InlineData("h(t) = 700 - 5t")]
        public void AsciiMath_Parser_Aint_Great(string input)
        {
            var x = AsciiMath.Parser.ToMathMl(input);
            var replaced = Assignment.ReplaceAsciiMathWithMathML($"`{input}`");
            replaced.ShouldContain("<math>");
        }

        private RawAssignment.Assignment GetRawAssignment(int assignmentId, RawAssignment.Root? root = null, string? filename = null)
        {
            root ??= (filename == null ? GetRawAssignments() : GetRawAssignments(filename));
            var rawAssignment = root!.Subpart.Select(o => o.Assignments.SingleOrDefault(a => a.AssignmentID == assignmentId)).Single();
            return rawAssignment!;
        }

        private RawAssignment.Root GetRawAssignments(string filename = "assignments_141094_16961.json")
        {
            return JsonConvert.DeserializeObject<RawAssignment.Root>(File.ReadAllText(Helpers.GetJsonFile(filename)))!;
        }
        public static List<Assignment> GetAssignments(string filename = "assignments_141094_16961.json")
        {
            var raw = JsonConvert.DeserializeObject<RawAssignment.Root>(File.ReadAllText(Helpers.GetJsonFile(filename)))!;
            return raw.Subpart.SelectMany(o => o.Assignments).Select(Assignment.Create).OfType<Assignment>().ToList();
        }

        private List<Subpart> LoadAllSubparts()
        {
            var filenames = new[] { "assignments_141094_16961.json", "assignment2.json", "someAssignment.json" };
            return filenames.SelectMany(o => RawConverter.ReadRaw(File.ReadAllText(Helpers.GetJsonFile(o)))).ToList();
        }
    }
}