using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoK.Models.Raw;

namespace NoK.Tests
{
    public class Course_Tests
    {
        [Fact]
        public void Course_CreateNodeTree()
        {
            var allCourses = new[] { "course_2982.json", "course_16961.json" } //course_16961 course_2982
                .Select(LoadCourse)
                .Select(Models.Product.Create)
                .ToList();
        }

        [Fact]
        public void Course_Deserialize()
        {
            var allCourses = new[] { "course_2982.json", "course_16961.json" } //course_16961 course_2982
                .Select(LoadCourse)
                .Select(o => o.Content)
                .ToList();

            var toc = allCourses.Select(course =>
                new JObject(course.Chapters.Select(chapter =>
                    new JProperty(chapter.Name, new JObject(chapter.Parts.Select(part =>
                        new JProperty(part.Name, new JObject(part.SubParts.Select(sp =>
                            new JProperty(sp.Name, sp.Sections.Select(sec => sec.Name))
                        )))
                    )))
                    )
                ))
                .ToList();

            //var subparts = LoadAllSubparts();
            //var assignmentById = subparts.SelectMany(o => o.Assignments)
            //    .ToDictionary(o => o.Id, o => o);

            //var aaa = allCourses.SelectMany(o =>
            //o.Chapters.SelectMany(p =>
            //p.Parts.SelectMany(q =>
            //q.SubParts.SelectMany(r =>
            //r.Sections.SelectMany(r => r.SectionAssignmentRelations ?? new List<RawCourse.SectionAssignmentRelation>()))))).ToList();

            //var joined = aaa.Select(o => o.AssignmentId == null ? null : assignmentById.TryGetValue(o.AssignmentId.Value, out var a) ? new { Assignment = a, Rel = o } : null)
            //    .Where(o => o != null)
            //    .ToList();
        }

        private RawCourse.Root LoadCourse(string filename)
        {
            var content = File.ReadAllText(Helpers.GetJsonFile(filename));
            var result = JsonConvert.DeserializeObject<RawCourse.Root>(content);
            if (result == null)
                throw new NullReferenceException(filename);
            return result;
        }
    }
}
