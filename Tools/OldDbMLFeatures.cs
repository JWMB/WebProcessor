using AngleSharp.Common;
using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OldDbAdapter;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using System.Globalization;
using static ProblemSource.Models.Aggregates.ColumnTypeAttribute;
using static Tools.MLDynamic;

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

        public IMLFeature CreateFeature(IEnumerable<Phase> phases, TrainingSettings trainingSettings, int age)
        {
            return MLFeaturesJulia.FromPhases(trainingSettings, phases, age, null, 5);
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

                //var renderer = new SqlGroupExpressionRenderer(new SqlGroupExpressionRenderer.M2MTableConfig(), new SqlGroupExpressionRenderer.GroupTableConfig());
                //var sqlAccountIds = BooleanExpressionTree.ParseAndRender($"\"{group}\"", renderer);
                //var accountIds = await db.Read(GetSqlForAccountsWithMinNumDays(20, $"AND account_id IN ({sqlAccountIds})"), (rd, cols) => rd.GetInt32(0));

                var qGroups = @"
SELECT account_id, name FROM accounts_groups
INNER JOIN groups ON groups.id = accounts_groups.group_id
  WHERE group_id IN (SELECT id FROM groups WHERE name LIKE '_Ages%')
  AND account_id IN (
  SELECT account_id
  FROM phases
  GROUP BY account_id
  HAVING MAX(training_day) >= 35
)
 ";
                var ages = (await db.Read(qGroups, (r, cs) => new { Id = r.GetInt32(0), Ages = r.GetString(1).ToLower().Replace("_ages ", "") }))
                    .ToDictionary(o => o.Id, o => int.Parse(o.Ages.Remove(o.Ages.IndexOf("-"))));
                //var ageCounts = ages.GroupBy(o => o.Value).ToDictionary(o => o.Key, o => o.Count());

                var features = new Dictionary<int, IMLFeature>();

                var includedAges = new List<int>();
                foreach (var id in trainingIds)
                {
                    var phases = await GetTrainingPhases(id, Path.Join(path, "Phases"));
                    if (phases.Any() == false)
                        continue;

                    var age = ages.GetValueOrDefault(id, 0);
                    if (age > 0)
                    {
                        var row = CreateFeature(phases, new TrainingSettings(), age);
                        if (row.IsValid)
                        {
                            features.Add(id, row);
                            includedAges.Add(age);
                        }
                    }
                }

                var ageCounts = includedAges.GroupBy(o => o).OrderBy(o => o.Key).Select(o => new { Age = o.Key, Count = o.Count() });

                //finalLevels = finalLevels.Order().ToList();
                //var numChunks = 5;
                //var chunkSize = finalLevels.Count / numChunks;
                //var limits = finalLevels.Chunk(chunkSize).Take(numChunks).Select(o => o.Last()).ToList();

                var firstFlat = features.First().Value.GetFlatFeatures();

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var columns = firstFlat.Keys.ToList();

                using (var writer = new StreamWriter(csvFile))
                {
                    writer.WriteLine(Join(new[] { "Id" }.Concat(columns)));

                    foreach (var kv in features)
                    {
                        var flat = kv.Value.GetFlatFeatures();
                        var row = new object[] { kv.Key }.Concat(columns.Select(o => flat[o]));
                        writer.WriteLine(Join(row));
                    }
                }

                string Join(IEnumerable<object?> values) => string.Join(",", values.Select(Render));
                string Render(object? value) => ConvertValue(value)?.ToString() ?? "";
                object? ConvertValue(object? value)
                {
                    if (value == null) return null;
                    if (value is bool b) return b ? 1 : 0;
                    if (value is float f) return f.ToString("0.####");
                    if (value is double d) return d.ToString("0.####");
                    if (value is decimal m) return m.ToString("0.####");
                    return value;
                }
            }

            var rootFeature = CreateFeature(new List<Phase>(), new TrainingSettings(), 0);
            var rootType = rootFeature.GetType();
            //CreateInstancesFromCsv(rootFeature.GetFlatFeatures(), csvFile);

            var columnTypePerProperty = rootType
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToDictionary(o => o.Name, o => (Attribute.GetCustomAttribute(o, typeof(ColumnTypeAttribute)) as ColumnTypeAttribute)?.Type);

            var colInfo = new ColumnInfo
            {
                Label = columnTypePerProperty.Single(o => o.Value == ColumnType.Label).Key,
                Categorical = columnTypePerProperty.Where(o => o.Value == ColumnType.Categorical).Select(o => o.Key),
                Ignore = columnTypePerProperty.Where(o => o.Value == ColumnType.Ignored).Select(o => o.Key),
                UserId = columnTypePerProperty.SingleOrDefault(o => o.Value == ColumnType.UserId).Key,
            };
            var ml = new MLDynamic(colInfo);

            var modelPath = Path.Join(path, "JuliaMLModel_Reg.zip");

            IExperimentConfig config = new RegressionExperimentConfig(colInfo);
            Action<TextLoader.Column[]>? modifyColumns = cols => cols.Single(o => o.Name == colInfo.Label).DataKind = DataKind.Single;
            //IExperimentConfig config = new MultiClassificationExperimentConfig(colInfo);
            //Action<TextLoader.Column[]>? modifyColumns = cols => cols.Single(o => o.Name == colInfo.Label).DataKind = DataKind.UInt32;

            // Load data regardless of training - so we can use Preview below
            ml.LoadData(new[] { csvFile }, modifyColumns);
            if (ml.TryLoad(modelPath) == false)
            {
                await ml.Train(config, TimeSpan.FromMinutes(60), cancellation);

                if (!string.IsNullOrEmpty(modelPath))
                    ml.Save(modelPath);
            }

            var rows = CreateDictionariesFromCsv(ml.Schema!, csvFile).ToList();
            //var rows = ml.DataView.Preview(100).RowView.Select(CreatePropertyDictFromPreviewRow).ToList();
            var table = GenerateAccuracyTable(rows, ml, colInfo);
        }

        private IEnumerable<Dictionary<string, object?>> CreateDictionariesFromCsv(DataViewSchema schema, string csvFile)
        {
            var rows = File.ReadAllLines(csvFile);
            var columnNames = rows.First().Split(',').ToList();

            var type = MLDynamic.CreateType(schema);
            var colToType = schema.ToDictionary(o => o.Name, o => o.Type.RawType);
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            return rows.Skip(1)
                .Select(row =>
                {
                    var vals = row.Split(',').ToList();
                    return columnNames
                        .Select((o, i) => new { Key = o, Value = vals[i] })
                        .ToDictionary(o => o.Key, o => Convert(o.Value, colToType[o.Key]));
                });

            object? Convert(string? value, Type type)
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (type.IsValueType)
                        return Activator.CreateInstance(type);
                    //return type.GetMethod("GetDefaultGeneric").MakeGenericMethod(t).Invoke(this, null);
                    //type = Nullable.GetUnderlyingType(type) ?? type;
                }
                return System.Convert.ChangeType(value, type);
            }
        }

        private Dictionary<string, object> CreatePropertyDictFromPreviewRow(DataDebuggerPreview.RowInfo row) =>
            row.Values.ToDictionary(o => o.Key, o => o.Value);


        private string GenerateAccuracyTable(List<Dictionary<string, object>> rows, MLDynamic ml, MLDynamic.ColumnInfo colInfo)
        {
            var type = CreateType(ml.Schema!);
            var instances = rows.Select(o => DynamicTypeFactory.CreateInstance(type, o));
            var predictions = ml.Predict(instances).ToList();

            var predictedPerRow = rows.Select((o, i) => new {
                Predicted = (float)Math.Round(Convert.ToSingle(predictions[i])),
                Actual = Convert.ToInt32(o[colInfo.Label]),
                Values = o
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

                return new[] { (decimal)row, (decimal)totalCnt }.Concat(percentages.Select(o => Math.Round(o * 100, 1))).ToList();
            }).ToList();

            var percentTable = string.Join("\n", numTable.Select(o => string.Join("\t", o)));
            return percentTable;
        }
    }
}
