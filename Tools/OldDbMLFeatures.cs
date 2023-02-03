using CsvHelper;
using OldDb.Models;
using OldDbAdapter;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using System.Globalization;

namespace Tools
{
    internal class OldDbMLFeatures
    {
        public async Task<object> CreateFeaturesForTraining(int trainingId)
        {
            var dbContext = new TrainingDbContext();
            var logItems = await RecreateLogFromOldDb.GetAsLogItems(dbContext, trainingId, Enumerable.Range(0, 6));

            var phases = LogEventsToPhases.Create(logItems);
            var features = MLFeaturesJulia.FromPhases(new TrainingSettings(), phases.PhasesCreated, 6, null, 5);
            return features.GetRelevantFeatures();
        }

        public async Task Run()
        {
            // TODO: some criteria
            var trainingIds = new List<int> {
                21528,
                21922,
                22356,
                23673,
                23678,
                25042,
                25049,
                25050,
                25053,
                27537,
            };

            var features = new List<object>();
            foreach (var id in trainingIds)
            {
                features.Add(await CreateFeaturesForTraining(id));
            }

            var path = @"C:\Users\uzk446\Downloads\";

            using (var writer = new StreamWriter(Path.Join(path, "aaa.csv")))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(features);
            }


            //new MLTyped().Test(Path.Join(path, "taxi-fare-train.csv"), Path.Join(path, "taxi-fare-test.csv"));
            new MLDynamic().TutorialTest(Path.Join(path, "taxi-fare-train.csv"), Path.Join(path, "taxi-fare-test.csv"),
                "fare_amount", new[] { "rate_code", "vendor_id", "payment_type" },
                Path.Join(path, "taxi-fare-model.zip"));
        }
    }
}
