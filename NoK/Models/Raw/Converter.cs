using Newtonsoft.Json;

namespace NoK.Models.Raw
{
    public class RawConverter
    {
        public static List<Subpart> ReadRaw(string json)
        {
            var root = JsonConvert.DeserializeObject<RawAssignment.Root>(json);
            if (root == null)
                throw new ArgumentException("Can't deserialize");

            return root.Subpart
                .Select(o => new Subpart {  Title = o.Name, LessonId = o.LessonId ?? 0, Assignments = o.Assignments.Select(Assignment.Create).ToList() })
                .ToList();
        }
    }
}
