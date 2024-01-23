using Newtonsoft.Json;
using NoK.Models.Raw;
using Shouldly;

namespace NoK.Tests
{
    public partial class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var subparts = RawConverter.ReadRaw(File.ReadAllText(@"C:\Users\jonas\Downloads\assignments_141094_16961\assignments_141094_16961.json"));
            var assignments = subparts.SelectMany(o => o.Assignments).ToList();

            var strange = assignments.SelectMany(o => o.Tasks).Where(o => o.Hint?.Count > 1 || o.Solution?.Count > 1);

            var questions = assignments.SelectMany(o => o.Tasks).Select(o => o.Question).ToList();
        }

        [Fact]
        public void ParseSolution_Wirisformula()
        {
            var json = "[{\"text\":\"<div><img alt=\\\"fraction numerator negative 150 over denominator 3 end fraction space equals space minus 50\\\" class=\\\"Wirisformula\\\" data-mathml=\\\"\\u00abmath xmlns=\\u00a8http:\\/\\/www.w3.org\\/1998\\/Math\\/MathML\\u00a8\\u00bb\\u00abmfrac\\u00bb\\u00abmrow\\u00bb\\u00abmo\\u00bb-\\u00ab\\/mo\\u00bb\\u00abmn\\u00bb150\\u00ab\\/mn\\u00bb\\u00ab\\/mrow\\u00bb\\u00abmn\\u00bb3\\u00ab\\/mn\\u00bb\\u00ab\\/mfrac\\u00bb\\u00abmo\\u00bb\\u00a7#160;\\u00ab\\/mo\\u00bb\\u00abmo\\u00bb=\\u00ab\\/mo\\u00bb\\u00abmo\\u00bb\\u00a7#160;\\u00ab\\/mo\\u00bb\\u00abmo\\u00bb-\\u00ab\\/mo\\u00bb\\u00abmn\\u00bb50\\u00ab\\/mn\\u00bb\\u00ab\\/math\\u00bb\\\" src=\\\"\\/editor\\/ckeditor4\\/\\/plugins\\/ckeditor_wiris\\/integration\\/showimage.php?formula=4be1d4c6af8142695e03d3f5c7fd18fe\\\" \\/><\\/div>\\n\\n<div>&nbsp;<\\/div>\"}]";
            var tmp = Assignment.ParseSolutions(json);
            tmp!.Single().ShouldStartWith("<math xmlns");
        }
    }
}