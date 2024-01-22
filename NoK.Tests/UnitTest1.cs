using Newtonsoft.Json;
using NoK.Models.Raw;

namespace NoK.Tests
{
    public partial class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var json = File.ReadAllText(@"C:\Users\jonas\Downloads\assignments_141094_16961\assignments_141094_16961.json");
            var root = JsonConvert.DeserializeObject<Root>(json);
            var oo = root!.Subpart.SelectMany(sp => sp.Assignments).ToList();
            var assignments = root!.Subpart.SelectMany(sp => sp.Assignments.Where(o => o.AssignmentID == 141087).Select(Ass.Create)).ToList();

            //var z = oo.SelectMany(o => {
            //    return Enumerable.Range(0, Math.Max(o.Hints?.Count ?? 0, o.Solutions?.Count ?? 0))
            //        .Select(i => new Subtask { Hint = Get(o.Hints, i), Solution = Get(o.Solutions, i) });
            //});
            //var off = z.Where(o => o.Hint == null || o.Solution == null).ToList();
            //var of2 = z.Where(o => o.Hint?.Subtask != o.Solution?.Subtask).ToList();
        }

        private T? Get<T>(IEnumerable<T>? items, int index) => items == null ? default : items.Skip(index).FirstOrDefault();
    }
}