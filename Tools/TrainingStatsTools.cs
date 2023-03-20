using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;

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

        public async Task OverallStats(int minTrainedDays)
        {
            //var states = await GetTrainingStates();
            //var trainingDayById = states.ToDictionary(o => o.Key, o => o.Value.exercise_stats.trainingDay);

            //var phasesById = await GetPhases($"training_day ge {minTrainedDays}");
            //var maxDays = phasesById.Select(o => new { Id = o.Key, MaxDay = o.Value.Max(o => o.training_day), MaxDayState = trainingDayById.GetValueOrDefault(o.Key, 0) }).ToList();

            string? partitionKey = null;
            var tableClientFactory = serviceProvider.GetRequiredService<ITypedTableClientFactory>();
            var trainingSummaries = new Lazy<IBatchRepository<TrainingSummary>>(() =>
                Create(new AutoConvertTableEntityRepository<TrainingSummary>(tableClientFactory.TrainingSummaries,
                new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
                t => partitionKey));
            var allSummaries = await trainingSummaries.Value.GetAll();

            var dateWhenAdjustedClientWasReleased = DateTimeOffset.Parse("2023-03-01");
            var withAdjustedClient = allSummaries
                .Where(o => o.FirstLogin >= dateWhenAdjustedClientWasReleased)
                .Where(o => o.TrainedDays >= minTrainedDays)
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

            var byAge = info.GroupBy(o => o.Age).ToDictionary(o => o.Key, o => o.Count());
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
    }
}
