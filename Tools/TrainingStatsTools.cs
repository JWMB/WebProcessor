using Azure.Data.Tables;
using AzureTableGenerics;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;
using ProblemSourceModule.Services.TrainingAnalyzers;
using System.Reflection;

namespace Tools
{
    internal class TrainingStatsTools
    {
        private readonly IServiceProvider serviceProvider;

        public TrainingStatsTools(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task GetDevices()
        {
            var states = await GetTrainingStates();
            var devices = states.Select(o => o.Value.exercise_stats.device.model)
                .Where(o => o.Any())
                .Select(JObject.Parse)
                .ToList();

            var userAgents = devices.Select(o => o.Value<string>("userAgent")).ToList();
        }

        public async Task<Dictionary<string, Dictionary<string, List<int>>>> GetTrainingIdsToTeachers(IEnumerable<int> trainingIds)
        {
            var allUsers = await serviceProvider.GetRequiredService<IUserRepository>().GetAll();
            var users = allUsers
                .Select(o => new
                {
                    User = o,
                    Trainings = o.Trainings.Select(p => new { Key = p.Key, Value = p.Value.Intersect(trainingIds) }).Where(o => o.Value.Any()).ToDictionary(o => o.Key, o => o.Value.ToList())
                })
                .Where(o => o.Trainings.Any())
                .ToDictionary(o => o.User.Email, o => o.Trainings);
            return users;
        }

        public IBatchRepository<TrainingSummary> CreateTrainingSummaryRepo()
        {
            string? partitionKey = null;

            var tableClientFactory = serviceProvider.GetRequiredService<ITypedTableClientFactory>();
            var trainingSummaries = new Lazy<IBatchRepository<TrainingSummary>>(() =>
                Create(new AutoConvertTableEntityRepositoryBatch<TrainingSummary>(tableClientFactory.TrainingSummaries,
            new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
            t => partitionKey));
            return trainingSummaries.Value;
        }

        public async Task OverallStats(int minTrainedDays)
        {
            //var states = await GetTrainingStates();
            //var trainingDayById = states.ToDictionary(o => o.Key, o => o.Value.exercise_stats.trainingDay);

            //var phasesById = await GetPhases($"training_day ge {minTrainedDays}");
            //var maxDays = phasesById.Select(o => new { Id = o.Key, MaxDay = o.Value.Max(o => o.training_day), MaxDayState = trainingDayById.GetValueOrDefault(o.Key, 0) }).ToList();

            //string? partitionKey = null;
            //var tableClientFactory = serviceProvider.GetRequiredService<ITypedTableClientFactory>();
            //var trainingSummaries = new Lazy<IBatchRepository<TrainingSummary>>(() =>
            //    Create(new AutoConvertTableEntityRepository<TrainingSummary>(tableClientFactory.TrainingSummaries,
            //    new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
            //    t => partitionKey));
            //var allSummaries = await trainingSummaries.Value.GetAll();
            var allSummaries = await CreateTrainingSummaryRepo().GetAll();
            //var start = DateTimeOffset.Parse("2023-01-01");
            //var numTrainingsAtLeastNDays = allSummaries.Count(o => o.TrainedDays >= minTrainedDays && o.FirstLogin >= start);

            var dateWhenAdjustedClientWasReleased = DateTimeOffset.Parse("2023-03-01");
            var withAdjustedClient = allSummaries
                .Where(o => o.FirstLogin >= dateWhenAdjustedClientWasReleased)
                //.Where(o => o.TrainedDays >= minTrainedDays)
                .ToList();

            var ids = withAdjustedClient.Select(o => o.Id).ToList();
            var trainingRepository = serviceProvider.GetRequiredService<ITrainingRepository>();
            var trainings = (await trainingRepository.GetAll())
                .Where(o => ids.Contains(o.Id))
                .ToList();
            var info = trainings.Select(o =>
            {
                var summary = withAdjustedClient.Single(p => p.Id == o.Id);
                return new { Id = o.Id, Days = summary.TrainedDays, Age = o.AgeBracket, Overrides = o.Settings.trainingPlanOverrides };
            }).ToList();

            var byAge = info.GroupBy(o => o.Age).Select(o => new { o.Key, Count = o.Count() }).OrderBy(o => o.Key);
            var byDays = info.GroupBy(o => o.Days).Select(o => new { o.Key, Count = o.Count() }).OrderBy(o => o.Key);
            var byOverrides = info.GroupBy(o => o.Overrides != null).ToDictionary(o => o.Key, o => o.Count());

            //var statisticsProvider = serviceProvider.CreateInstance<StatisticsProvider>();
            //var toInvestigate = allSummaries.Where(o => o.TrainedDays > 1).OrderByDescending(o => o.Id).ToList();
            //foreach (var item in toInvestigate)
            //{
            //    var phaseStatistics = await statisticsProvider.GetPhaseStatistics(item.Id);
            //    var byExercise = phaseStatistics.GroupBy(o => o.exercise).ToDictionary(o => o.Key, o => o.OrderBy(p => p.training_day).ToList());

            //    var aa = byExercise.Select(o => new { Exercise = o.Key, LevelSpan = string.Join(",", o.Value.Select(o => $"{o.training_day}:{(int)o.level_min}-{(int)o.level_max}")) }).ToList();
            //}
        }

        private async Task<Dictionary<int, List<PhaseTableEntity>>> GetPhases(string filter)
        {
            var tableClientFactory = serviceProvider.GetRequiredService<ITypedTableClientFactory>();

            var query = tableClientFactory.Phases.QueryAsync<PhaseTableEntity>(filter);
            var phasesById = new Dictionary<int, List<PhaseTableEntity>>();
            await foreach (var entity in query)
            {
                var trainingId = int.Parse(entity.PartitionKey.TrimStart('0'));
                if (!phasesById.TryGetValue(trainingId, out var list))
                {
                    list = new List<PhaseTableEntity>();
                    phasesById[trainingId] = list;
                }
                list.Add(entity);
            }
            return phasesById;
        }

        private async Task<Dictionary<int, UserGeneratedState>> GetTrainingStates()
        {
            string? partitionKey = null;
            var tableClientFactory = serviceProvider.GetRequiredService<ITypedTableClientFactory>();
            var queryStates = tableClientFactory.UserStates.QueryAsync<TableEntity>();
            var converter = new ExpandableTableEntityConverter<UserGeneratedState>(t => new TableFilter("none", partitionKey));
            var stateById = new Dictionary<int, UserGeneratedState>();
            await foreach (var entity in queryStates)
            {
                var converted = converter.ToPoco(entity);
                //var userStats = converted.exercise_stats;
                stateById.Add(int.Parse(entity.RowKey.TrimStart('0')), converted);
            }
            return stateById;
        }

        private IBatchRepository<T> Create<T>(IBatchRepository<T> inner, Func<T, string> createKey) => inner;

        public async Task<List<string>> GetUsersWithSyncedTrainings()
        {
            var userRepo = serviceProvider.CreateInstance<AzureTableUserRepository>();
            var users = await userRepo.GetAll();
            var userToTrainings = users.Where(o => o.Role == "Teacher").ToDictionary(o => o.Email, o => o.Trainings.SelectMany(p => p.Value));
            var trainingToUsers = userToTrainings.SelectMany(o => o.Value.Select(p => new { TrainingId = p, User = o.Key }))
                .GroupBy(o => o.TrainingId).Select(o => new { TrainingId = o.Key, Users = o.Select(p => p.User).ToList() })
                .ToDictionary(o => o.TrainingId, o => o.Users);

            var allTrainingsToCheck = userToTrainings.Values.SelectMany(o => o).ToList();

            var statisticsProvider = serviceProvider.CreateInstance<StatisticsProvider>();
            var summaries = (await statisticsProvider.GetTrainingSummaries(allTrainingsToCheck)).OfType<TrainingSummary>();

            var summaryToUser = summaries.Select(o => new { Summary = o, Users = trainingToUsers[o.Id] }).ToList();
            var allUsersWithSummaries = summaryToUser.SelectMany(o => o.Users).Distinct().ToList();

            return allUsersWithSummaries;
        }

        public async Task ExportTrainingsKIFormat()
        {
            var path = @"C:\temp\_KIExport";

            var trainingsRepo = serviceProvider.CreateInstance<AzureTableTrainingRepository>();

            var minNumDays = 15;
            List<Training> allTrainings;
            if (true)
            {
                var earliestStart = new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.Zero);
                var allSummaries = await CreateTrainingSummaryRepo().GetAll();
                var includedSummaries = allSummaries
                    //.Where(o => o.TrainedDays >= 35)
                    .Where(o => o.TrainedDays >= 6)
                    .Where(o => o.FirstLogin > earliestStart)
                    .ToList();
                var trainingIds = includedSummaries.Select(o => o.Id).ToList();
                //trainingIds = new List<int> { 16254, 16258 };

                allTrainings = (await trainingsRepo.GetByIds(trainingIds)).ToList();

                var selectedTrainings = allTrainings
                    .Where(training => training.Settings.trainingPlanOverrides != null)
                    .Where(training => training.TrainingPlanName == "2023 HT template")
                    .ToList();

                var selectedIds = selectedTrainings.Select(o => o.Id).ToList();
                var joined = selectedTrainings.Join(allSummaries, o => o.Id, o => o.Id, (t, s) => new { Training = t, Summary = s }).ToList();//.Where(o => selectedIds.Contains(o.Id)).ToList();

                var dbgStats = string.Join("\n", joined.Select(o => string.Join("\t", new object[] { 
                    o.Training.Id, o.Summary.TrainedDays, o.Summary.FirstLogin, o.Summary.LastLogin, o.Summary.AvgDaysPerWeek.ToString("#.0"), o.Training.TrainingPlanName,
                }.Select(o => o.ToString()))));

                allTrainings = selectedTrainings;
                //withOverrides.Where(o => o.TrainingPlanName == "")
            }
            else
            {
                allTrainings = (await trainingsRepo.GetAll()).ToList();
            }

            Console.WriteLine($"Including {allTrainings.Count} trainings");
            
            var xrepo = serviceProvider.CreateInstance<AzureTableUserGeneratedDataRepositoriesProviderFactory>();
            var properties = new List<PropertyInfo>();

            var predictor = new CachedPredictor(path);

            using var fileStream = File.OpenWrite(Path.Combine(path, "KIExport.csv"));
            using var writer = new StreamWriter(fileStream);

            var dbg = new List<string>();
            foreach (var training in allTrainings)
            {
                if (false)
                {
                    var tmp = GetMods(training);
                    dbg.Add($"{training.Id}\t{tmp.numberlineModEasy}\t{tmp.weightMod}");
                }
                else
                {
                    var rows = await KITrainingExport(training, xrepo.Create(training.Id), predictor, minNumDays);

                    if (rows.Count < 100)
                        continue;

                    if (!properties.Any())
                    {
                        var row = rows[0];
                        properties = row.GetType().GetProperties().ToList();

                        await writer.WriteAsync(string.Join(",", properties.Select(o => o.Name)));
                        await writer.WriteAsync("\n");
                    }

                    await writer.WriteAsync(
                        string.Join("\n",
                        rows.Select(row => string.Join(",", properties.Select(o => o.GetValue(row)?.ToString())))
                        ));
                    await writer.WriteAsync("\n");
                    await writer.FlushAsync();

                    Console.WriteLine($"{training.Id} ({100 * allTrainings.IndexOf(training) / allTrainings.Count} {allTrainings.Count})");
                }
            }
            var x = string.Join("\n", dbg);
        }

        private (bool numberlineModEasy, string weightMod) GetMods(Training training)
        {
            var numberlineModEasy = false;
            var weightMod = "";
            var settings = training.Settings ?? new TrainingSettings();
            if (settings.trainingPlanOverrides != null)
            {
                dynamic overrides = settings.trainingPlanOverrides;
                if (overrides.triggers != null && overrides.triggers[0] != null && overrides.triggers[0].actionData != null)
                {
                    dynamic props = overrides.triggers[0].actionData.properties;
                    if (props != null)
                    {
                        var weights = props.weights as JObject;
                        if (weights != null)
                        {
                            weightMod = string.Join("", new[] { "WM", "Math", "NVR", "Reasoning" }.Select(o => $"{o[0]}{weights[o]?.Value<int>() ?? 0}"));
                        }

                        var phasesx = props.phases as JObject;
                        if (phasesx != null)
                        {
                            var numberlineMod = phasesx.Children().OfType<JProperty>()
                                .FirstOrDefault(o => o.Name.StartsWith("numberline"))?
                                .Descendants().OfType<JProperty>().FirstOrDefault(o => o.Name == "path")?
                                .FirstOrDefault()?.Value<string>() ?? "";
                            numberlineModEasy = numberlineMod.Contains("easy_ola");
                        }
                    }
                }
            }
            return (numberlineModEasy, weightMod);
        }

        private async Task<List<object>> KITrainingExport(Training training, IUserGeneratedDataRepositoryProvider provider, CachedPredictor predictor, int? minNumDays = null)
        {
            var phases = (await provider.Phases.GetAll()).ToList();

            var tdays = phases.Select(o => o.training_day).Distinct().ToList();
            if (minNumDays.HasValue && (tdays.Count == 0 || tdays.Max() < minNumDays.Value))
                return new List<object>();

            var predicted = await predictor.Predict(training, phases);

            var (numberlineModEasy, weightMod) = GetMods(training);
            //var numberlineModEasy = false;
            //var weightMod = "";
            var settings = training.Settings ?? new TrainingSettings();
            //if (settings.trainingPlanOverrides != null)
            //{
            //    dynamic overrides = settings.trainingPlanOverrides;
            //    if (overrides.triggers != null && overrides.triggers[0] != null && overrides.triggers[0].actionData != null)
            //    {
            //        dynamic props = overrides.triggers[0].actionData.properties;
            //        if (props != null)
            //        {
            //            var weights = props.weights as JObject;
            //            if (weights != null)
            //            {
            //                weightMod = string.Join("", new[] { "WM", "Math", "NVR" }.Select(o => $"{o[0]}{weights[o]?.Value<int>() ?? 0}"));
            //            }

            //            var phasesx = props.phases as JObject;
            //            if (phasesx != null)
            //            {
            //                var numberlineMod = phasesx.Children().OfType<JProperty>()
            //                    .FirstOrDefault(o => o.Name.StartsWith("numberline"))?
            //                    .Descendants().OfType<JProperty>().FirstOrDefault(o => o.Name == "path")?
            //                    .FirstOrDefault()?.Value<string>() ?? "";
            //                numberlineModEasy = numberlineMod.Contains("easy_ola");
            //            }
            //        }
            //    }
            //}


            return phases.OrderBy(o => o.training_day).ThenBy(o => o.time).SelectMany(phase =>
                phase.problems.OrderBy(o => o.time).SelectMany(problem =>
                    problem.answers.OrderBy(o => o.time).Select(answer =>
                    (object)new
                    {
                        account_id = training.Id,
                        exercice = phase.exercise.Replace("#intro", ""),
                        correct = answer.correct ? 1 : 0,
                        problem_time = answer.time,
                        problem_string = $"\"{problem.problem_string.Replace("\"", "'")}\"",
                        level = problem.level.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture.NumberFormat),
                        training_plan_id = 0,
                        targetTime = settings.timeLimits.FirstOrDefault().ToString("0.#", System.Globalization.CultureInfo.InvariantCulture.NumberFormat),
                        phase.training_day,
                        answer.response_time,
                        answer.tries,
                        is_intro = phase.exercise.EndsWith("#intro") ? 1 : 0,
                        numberline_mod = numberlineModEasy ? 1 : 0,
                        weight_mod = weightMod,
                        age_lower = training.GetAgeBracketLower(),
                        predicted_level = predicted.Predicted.HasValue ? (int?)predicted.Predicted : null,
                        predicted_tier = predicted.PredictedPerformanceTier.ToString()
                    })))
                .ToList();
        }
    }

    public class CachedPredictor
    {
        private Dictionary<int, int> cached = new();

        private IPredictNumberlineLevelService inner;
        public CachedPredictor(string path)
        {
            inner = new MLPredictNumberlineLevelService(new LocalMLPredictor(Path.Combine(path, "JuliaMLModel_Reg.zip")));
            var predictionsFile = Path.Combine(path, "_predictions.csv");
            if (File.Exists(predictionsFile))
            {
                cached = File.ReadAllText(predictionsFile)
                    .Split('\n')
                    .Select(o => o.Trim().Split(','))
                    .Where(o => o.Length == 2)
                    .ToDictionary(o => int.Parse(o[0]), o => int.Parse(o[1]));
            }
        }

        public async Task<PredictedNumberlineLevel> Predict(Training training, List<Phase> phases)
        {
            if (cached.TryGetValue(training.Id, out var prediction))
                return new PredictedNumberlineLevel { Predicted = prediction };

            var features = MLFeaturesJulia.FromPhases(training.Settings, phases, training.GetAgeBracketLower(), null, 5);
            if (features.InvalidReasons.Any())
                return new PredictedNumberlineLevel { Predicted = null };

            var predicted = await inner.Predict(features);
            if (!predicted.Predicted.HasValue)
            { }
            return predicted;
        }
    }
}
