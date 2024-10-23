using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoK.Models;
using NoK.Models.Raw;
using Shouldly;

namespace NoK.Tests
{
    public class Course_Tests
    {
        [Fact]
        public void Course_CreateNodeTree()
        {
            var allCourses = new[] { "course_2982.json", "course_16961.json" } //course_16961 course_2982
                .Select(LoadCourse)
                .Select(ProductNode.Create)
                .ToList();

            var lessons = allCourses.SelectMany(o => o.Descendants().OfType<SectionNode>()).ToList();

            var assignments = Assignment_Tests.GetAssignments();
            foreach (var item in assignments)
            {
                var lesson = lessons.FirstOrDefault(o => o.AssignmentIds.Contains(item.Id));
                lesson.ShouldNotBeNull();
                lesson.Parent!.GetType().ShouldBe(typeof(SubpartNode));
            }

            var aaa = allCourses.Select(Rec);
            
            var r = System.Text.Json.JsonSerializer.Serialize(aaa.First(), options: new System.Text.Json.JsonSerializerOptions{ WriteIndented = true } );
            System.Text.Json.Nodes.JsonObject Rec(ContentNode node)
            {
                //var el = System.Text.Json.JsonSerializer.SerializeToElement(node);
                //var jnode = System.Text.Json.Nodes.JsonObject.Create(el);
                //if (jnode == null)
                //    throw new Exception($"Could not create JsonObject {node.Id}");
                //jnode.Remove(nameof(ContentNode.Parent));

                var jnode = new System.Text.Json.Nodes.JsonObject();
                jnode["Name"] = $"{node.GetType().Name} - {node.Name}";

                var children = new System.Text.Json.Nodes.JsonArray();
                foreach (var child in node.Children())
                    children.Add(Rec(child));
                if (children.Any())
                    jnode["Children"] = children;
                return jnode;
            }
        }

        [Fact]
        public void FindUnparseableMathFormulas()
        {
            var allCourses = new[] { "course_2982.json", "course_16961.json" }
                .Select(LoadCourse)
                .ToList();
            var lessons = allCourses.SelectMany(crs => 
                crs.Content.Chapters.SelectMany(c => 
                    c.Parts.SelectMany(s => 
                        s.SubParts.SelectMany(sp => 
                            sp.Sections.Select(se => se.Lesson))))).ToList();
            var aaa = lessons.Select(o =>
            {
                try
                {
                    Assignment.ReplaceAsciiMathWithMathML(o.Html, false);
                }
                catch (Exception ex)
                {
                    return $"{ex.Message} -- {o.Id}\n{o.Html}";
                }
                return null;
            }).Where(o => o != null) // OfType<string>()
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
