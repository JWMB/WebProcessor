using AngleSharp.Io;
using Newtonsoft.Json;
using NoK.Models;
using NoK.Models.Raw;
using Shouldly;

namespace NoK.Tests
{
    public class Assignment_Tests
    {
        [Fact]
        public void SubParts_Deserialize()
        {
            var subparts = LoadAllSubparts();
            var assignments = subparts.SelectMany(o => o.Assignments).ToList();

            var byUnit = assignments.OfType<Assignment>().GroupBy(o => o.Unit ?? "").ToDictionary(o => o.Key, o => o.ToList());
            var byType = assignments.OfType<Assignment>().GroupBy(o => o.ResponseType).ToDictionary(o => o.Key, o => o.ToList());
            //var strange = assignments.SelectMany(o => o.Tasks).Where(o => o.Hint?.Count > 1 || o.Solution?.Count > 1);
            //var questions = assignments.SelectMany(o => o.Tasks).Select(o => o.Question).ToList();

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

        [Fact]
        public void Course_Deserialize()
        {
            var dir = new DirectoryInfo("C:\\Users\\jonas\\Downloads\\assignments_141094_16961");

            var allCourses = new[] { "course_2982.json", "course_2982.json" }
                .Select(o => File.ReadAllText(Path.Join(dir.FullName, o)))
                .Select(JsonConvert.DeserializeObject<RawCourse.Root>).OfType<RawCourse.Root>()
                .Select(o => o.Content)
                .ToList();

            var subparts = LoadAllSubparts();
            var assignmentById = subparts.SelectMany(o => o.Assignments)
                .ToDictionary(o => o.Id, o => o);

            var aaa = allCourses.SelectMany(o =>
            o.Chapters.SelectMany(p =>
            p.Parts.SelectMany(q =>
            q.SubParts.SelectMany(r =>
            r.Sections.SelectMany(r => r.SectionAssignmentRelations ?? new List<RawCourse.SectionAssignmentRelation>()))))).ToList();

            var joined = aaa.Select(o => o.AssignmentId == null ? null : assignmentById.TryGetValue(o.AssignmentId.Value, out var a) ? new { Assignment = a, Rel = o } : null)
                .Where(o => o != null)
                .ToList();
        }

        private List<Subpart> LoadAllSubparts()
        {
            var dir = new DirectoryInfo("C:\\Users\\jonas\\Downloads\\assignments_141094_16961");
            var filenames = new[] { "assignments_141094_16961.json", "assignment2.json", "someAssignment.json" };
            return filenames.SelectMany(o => RawConverter.ReadRaw(File.ReadAllText(Path.Join(dir.FullName, o)))).ToList();
        }
    }
}