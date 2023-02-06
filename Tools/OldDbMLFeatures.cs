using AngleSharp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OldDbAdapter;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using System;
using System.Globalization;
using static ProblemSource.Models.Aggregates.ColumnTypeAttribute;

namespace Tools
{
    internal class OldDbMLFeatures
    {
        public async Task<List<Phase>> GetTrainingPhases(int trainingId, string folderBasePath)
        {
            var scoringDays = Enumerable.Range(33, 4);
            var days = Enumerable.Range(0, 6).Concat(scoringDays);

            var folderPath = $"{folderBasePath}_{string.Join("-", days)}";
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filename = Path.Join(folderPath, $"{trainingId}.json");
            List<Phase> phases;
            if (File.Exists(filename))
            {
                phases = JsonConvert.DeserializeObject<List<Phase>>(File.ReadAllText(filename));
                if (phases == null)
                    throw new Exception($"Couldn't deserialized '{filename}'");
            }
            else
            {
                var dbContext = new OldDb.Models.TrainingDbContext();

                var logItems = await RecreateLogFromOldDb.GetAsLogItems(dbContext, trainingId, days);

                phases = LogEventsToPhases.Create(logItems).PhasesCreated;

                File.WriteAllText(filename, JsonConvert.SerializeObject(phases));
            }

            var foundDays = phases.Select(o => o.training_day).Distinct().ToList();
            if (scoringDays.Intersect(foundDays).Any() == false
                || phases.Count < 10
                || phases.Take(10).SelectMany(o => o.problems).Count() < 5
                )
                phases = new List<Phase>(); // Mark as not usable
            else if (phases.Where(o => o.training_day < 10 && o.exercise == "numberComparison01").Any() == false)
                phases = new List<Phase>(); // Mark as not usable

            return phases;
        }

        public async Task Run(CancellationToken cancellation = default)
        {
            var path = @"C:\Users\uzk446\Downloads\";
            var csvFile = Path.Join(path, "AllTrainings.csv");

            var rootType = typeof(MLFeaturesJulia);

            if (!File.Exists(csvFile))
            {
                var q = @"
SELECT DISTINCT(account_id) AS id -- [account_id] --, MAX(other_id) as maxDay
  FROM [trainingdb].[dbo].[aggregated_data]
  WHERE aggregator_id = 2 AND [latest_underlying] > '2017-01-01'
  GROUP BY account_id
  HAVING MAX(other_id) >= 35
  ORDER BY account_id";

                q = @"
SELECT account_id, MAX(training_day)
  FROM phases
  GROUP BY account_id
  HAVING MAX(training_day) >= 35
";

                var dbContext = new OldDb.Models.TrainingDbContext();
                var db = new DbSql(dbContext.Database.GetConnectionString()!);
                var trainingIds = await db.Read(q, (r, cs) => r.GetInt32(0));

                //var trainingIds = new List<int> { 84233, 85045, 86250, 88567, 89587, 89598, };

                var features = new Dictionary<int, object>();

                //var finalLevels = new List<float>();
                foreach (var id in trainingIds)
                {
                    var phases = await GetTrainingPhases(id, Path.Join(path, "Phases"));
                    if (phases.Any() == false)
                        continue;
                    var row = MLFeaturesJulia.FromPhases(new TrainingSettings(), phases, 6, null, 5);

                    if (row.IsValid)
                        features.Add(id, row.GetRelevantFeatures());
                }

                //finalLevels = finalLevels.Order().ToList();
                //var numChunks = 5;
                //var chunkSize = finalLevels.Count / numChunks;
                //var limits = finalLevels.Chunk(chunkSize).Take(numChunks).Select(o => o.Last()).ToList();

                if (features.First().Value is JObject jObj)
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    var columns = jObj.Properties().Select(o => o.Name).ToList();

                    using (var writer = new StreamWriter(csvFile))
                    {
                        writer.WriteLine(Join(new[] { "Id" }.Concat(columns)));

                        foreach (var kv in features)
                        {
                            var row = new object[] { kv.Key }.Concat(columns.Select(o => ((JObject)kv.Value).GetValue(o)));
                            writer.WriteLine(Join(row));
                        }
                    }

                    string Join(IEnumerable<object?> values) => string.Join(",", values.Select(Render));
                    string Render(object? value) => ConvertValue(value)?.ToString() ?? "";
                    object? ConvertValue(object? value)
                    {
                        if (value is JValue jv)
                        {
                            if (jv.Type == JTokenType.Boolean)
                                value = jv.Value<bool>();
                            else if (jv.Type == JTokenType.Float)
                                value = jv.Value<float>().ToString("0.####");
                        }
                        if (value is bool b)
                            return b ? 1 : 0;
                        return value;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var columnTypePerProperty = rootType
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToDictionary(o => o.Name, o => (Attribute.GetCustomAttribute(o, typeof(ColumnTypeAttribute)) as ColumnTypeAttribute)?.Type);

            var colInfo = new MLDynamic.ColumnInfo
            {
                Label = columnTypePerProperty.Single(o => o.Value == ColumnType.Label).Key,
                Categorical = columnTypePerProperty.Where(o => o.Value == ColumnType.Categorical).Select(o => o.Key),
                Ignore = columnTypePerProperty.Where(o => o.Value == ColumnType.Ignored).Select(o => o.Key),
            };
            var ml = new MLDynamic(colInfo);

            var modelPath = Path.Join(path, "JuliaMLModel.zip");
            if (ml.TryLoad(modelPath))
            {
                ml.LoadData(new[] { csvFile });
            }
            else
            {
                await ml.Train(new[] { csvFile }, modelPath, TimeSpan.FromMinutes(60), cancellation);
            }

            var preview = ml.DataView.Preview(1000);

            var diffs = preview.RowView
                .Select(r => {
                    var dict = r.Values.ToDictionary(o => o.Key, o => o.Value);
                    var forPredict = ClassFactory.CreateInstance(dict);

                    var predicted = ml.Predict(forPredict);
                    return new {
                        Id = dict["Id"],
                        Predicted = predicted,
                        Actual = dict[colInfo.Label],
                        Diff = (float)dict[colInfo.Label] - Convert.ToSingle(predicted),
                        RoundDiff = Convert.ToInt32(dict[colInfo.Label]) - (int)Math.Round(Convert.ToSingle(predicted))
                    };
                }).OrderByDescending(o => Math.Abs(o.RoundDiff))
            .ToList();

            var totalCount = diffs.Count;
            Console.WriteLine(string.Join("\n,", diffs.GroupBy(o => o.RoundDiff).ToDictionary(o => o.Key, o => 100 * o.Count() / totalCount)));

            var chunkErrors = Enumerable.Range(0, 5)
                .Select(chunk => { 
                    var inChunk = diffs.Where(o => Convert.ToInt32(o.Actual) == chunk); 
                    return new { Chunk = chunk, Errors = inChunk.GroupBy(o => o.RoundDiff).ToDictionary(o => o.Key, o => 100 * o.Count() / inChunk.Count()) };
                }).ToList();
            var errSizes = chunkErrors.SelectMany(o => o.Errors.Keys).Distinct().OrderByDescending(Math.Abs);

            var rows = chunkErrors.Select(chunk => {
                return new[] { chunk.Chunk }.Concat(errSizes.Select(o => chunk.Errors.GetValueOrDefault(o, 0))).ToList();
            }).ToList();

            var table = string.Join("\n",
                new[] { errSizes.ToList() }.Concat(rows)
                    .Select(r => string.Join("\t", r))
                    );
        }
    }
}
