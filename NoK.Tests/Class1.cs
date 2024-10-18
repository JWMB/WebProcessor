using Newtonsoft.Json;
using NoK.Models;
using NoK.Models.Raw;
using ProblemSourceModule.Services.ProblemGenerators;
using Shouldly;

namespace NoK.Tests
{
    public class Class1
    {
        [Fact]
        public async Task X()
        {
			IStimuliRepository problemRepository = new NoKStimuliRepository(new NoKStimuliRepository.Config(GetSourceFile()));
            await problemRepository.Init();

            var stimulus = await problemRepository.GetById("141087/0"); // 141087 55224
            stimulus.ShouldNotBeNull();

            var source = await ((NoKStimuliRepository)problemRepository).GetSource("147964");

			var checker = new NoKSolutionChecker((NoKStimuliRepository)problemRepository);
            var analysis = await checker.Check(stimulus, new SimpleUserResponse { ResponseText = "21" });
            analysis.IsCorrect.ShouldBeTrue();
        }

        [Theory]
		[InlineData(147964, 3)]
		[InlineData(147963, 2)]
		[InlineData(141107, 3)]
		[InlineData(141109, 4)]
		public async Task Investigate_InlineTasks(int assignmentId, int expectedNumTasks)
        {
            var rawAssignment = await GetRawAssignment(assignmentId);

            var converted = Assignment.Create(rawAssignment!);
			converted.Tasks.Where(o => string.IsNullOrEmpty(o.Question)).ShouldBeEmpty();
			converted.Tasks.Count.ShouldBe(expectedNumTasks);
		}

        [Fact]
		public async Task Investigate_MathML()
		{
			var assignmentId = 141091;
            var rawAssignment = await GetRawAssignment(assignmentId);
            var converted = Assignment.Create(rawAssignment!);

        }

        private async Task<RawAssignment.Assignment> GetRawAssignment(int assignmentId, RawAssignment.Root? root = null)
		{
            root ??= JsonConvert.DeserializeObject<RawAssignment.Root>(await File.ReadAllTextAsync(GetSourceFile()));
            var rawAssignment = root!.Subpart.Select(o => o.Assignments.SingleOrDefault(a => a.AssignmentID == assignmentId)).Single();
			return rawAssignment;
        }

        private string GetSourceFile()
        {
			var pathToFile = @"C:\Users\jonas\Downloads\assignments_141094_16961\assignments_141094_16961.json";
			var desktop = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			if (desktop.Exists)
			{
				var file = desktop.GetFiles("assignments_141094_16961.json").FirstOrDefault();
				if (file?.Exists == true)
					pathToFile = file.FullName;
			}
            return pathToFile;
		}
	}
}
