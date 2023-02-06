using AngleSharp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
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

        public IMLFeature CreateFeature(IEnumerable<Phase> phases, TrainingSettings trainingSettings)
        {
            return MLFeaturesJulia.FromPhases(trainingSettings, phases, 6, null, 5);
        }

        public async Task Run(CancellationToken cancellation = default)
        {
            var path = @"C:\Users\uzk446\Downloads\";
            var csvFile = Path.Join(path, "AllTrainings.csv");

            if (!File.Exists(csvFile))
            {
//                var q = @"
//SELECT DISTINCT(account_id) AS id -- [account_id] --, MAX(other_id) as maxDay
//  FROM [trainingdb].[dbo].[aggregated_data]
//  WHERE aggregator_id = 2 AND [latest_underlying] > '2017-01-01'
//  GROUP BY account_id
//  HAVING MAX(other_id) >= 35
//  ORDER BY account_id";

                var q = @"
SELECT account_id, MAX(training_day)
  FROM phases
  GROUP BY account_id
  HAVING MAX(training_day) >= 35
";

                var dbContext = new OldDb.Models.TrainingDbContext();
                var db = new DbSql(dbContext.Database.GetConnectionString()!);
                var trainingIds = await db.Read(q, (r, cs) => r.GetInt32(0));

                var features = new Dictionary<int, IMLFeature>();

                foreach (var id in trainingIds)
                {
                    var phases = await GetTrainingPhases(id, Path.Join(path, "Phases"));
                    if (phases.Any() == false)
                        continue;
                    var row = CreateFeature(phases, new TrainingSettings());
                    //var row = MLFeaturesJulia.FromPhases(new TrainingSettings(), phases, 6, null, 5);

                    if (row.IsValid)
                        features.Add(id, row);
                }

                //finalLevels = finalLevels.Order().ToList();
                //var numChunks = 5;
                //var chunkSize = finalLevels.Count / numChunks;
                //var limits = finalLevels.Chunk(chunkSize).Take(numChunks).Select(o => o.Last()).ToList();

                var firstFlat = features.First().Value.GetFlatFeatures();

                if (firstFlat is JObject jObj)
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    var columns = jObj.Properties().Select(o => o.Name).ToList();

                    using (var writer = new StreamWriter(csvFile))
                    {
                        writer.WriteLine(Join(new[] { "Id" }.Concat(columns)));

                        foreach (var kv in features)
                        {
                            var flat = (JObject)kv.Value.GetFlatFeatures();
                            var row = new object[] { kv.Key }.Concat(columns.Select(flat.GetValue));
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

            //var rootType = typeof(MLFeaturesJulia);
            var rootType = CreateFeature(new List<Phase>(), new TrainingSettings()).GetType();

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

            var table = GenerateAccuracyTable(preview, ml, colInfo);
        }

        string GenerateAccuracyTable(DataDebuggerPreview preview, MLDynamic ml, MLDynamic.ColumnInfo colInfo)
        {
            var predictedPerRow = preview.RowView.Select(r => {
                var dict = r.Values.ToDictionary(o => o.Key, o => o.Value);
                var forPredict = ClassFactory.CreateInstance(dict);

                var predicted = ml.Predict(forPredict);

                return new
                {
                    Row = r,
                    Predicted = (float)Math.Round(Convert.ToSingle(predicted)),
                    Actual = Convert.ToInt32(dict[colInfo.Label]),
                    Values = dict
                };
            }).ToList();

            var groupedByErrors = predictedPerRow
                .GroupBy(o => o.Actual)
                .Select(o => new { Group = o.Key, Distribution = o.GroupBy(p => (int)Math.Round(p.Predicted)).Select(p => new { Predicted = p.Key, Count = p.Count() }) })
                .ToList();

            var groups = groupedByErrors.Select(o => o.Group).Order().ToList();
            var numTable = groups.Select(row => {
                var grp = groupedByErrors.FirstOrDefault(o => o.Group == row);
                IEnumerable<decimal> percentages;
                int totalCnt = 0;
                if (grp != null)
                {
                    totalCnt = grp.Distribution.Sum(o => o.Count);
                    percentages = groups.Select(col => 1M * (grp.Distribution.FirstOrDefault(o => o.Predicted == col)?.Count ?? 0) / totalCnt);
                }
                else
                    percentages = groups.Select(o => 0M);

                return new[] { (decimal)row, (decimal)totalCnt }.Concat(percentages.Select(o => Math.Round(o * 100, 2))).ToList();
            }).ToList();

            var percentTable = string.Join("\n", numTable.Select(o => string.Join("\t", o)));
            return percentTable;

            //var diffs = preview.RowView
            //    .Select(r => {
            //        var dict = r.Values.ToDictionary(o => o.Key, o => o.Value);
            //        var forPredict = ClassFactory.CreateInstance(dict);

            //        var predicted = ml.Predict(forPredict);
            //        return new
            //        {
            //            Id = dict["Id"],
            //            Predicted = predicted,
            //            Actual = dict[colInfo.Label],
            //            Diff = (float)dict[colInfo.Label] - Convert.ToSingle(predicted),
            //            RoundDiff = Convert.ToInt32(dict[colInfo.Label]) - (int)Math.Round(Convert.ToSingle(predicted))
            //        };
            //    }).OrderByDescending(o => Math.Abs(o.RoundDiff))
            //.ToList();
        }
    }
}
