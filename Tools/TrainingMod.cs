using Azure.Data.Tables;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource;
using ProblemSourceModule.Services.Storage.AzureTables;
using System.Text.RegularExpressions;
using ProblemSource.Models;
using ProblemSourceModule.Services.Storage;

namespace Tools
{
    internal class TrainingMod
    {
        private readonly ITrainingRepository trainingRepository;
        private readonly IUserRepository userRepository;

        public TrainingMod(ITrainingRepository trainingRepository, IUserRepository userRepository)
        {
            this.trainingRepository = trainingRepository;
            this.userRepository = userRepository;
        }


        public async Task<List<int>> GetTrainingsForTeacher(string email, IEnumerable<string>? groups = null)
        {
            var user = await userRepository.Get(email);
            if (user == null) throw new Exception($"User not found");
            if (user.Trainings.Any() == false)
                return new List<int>();

            return (groups?.Any() == true
                ? user.Trainings.Where(o => groups?.Contains(o.Key) == true).SelectMany(o => o.Value)
                : user.Trainings.SelectMany(o => o.Value)
                ).ToList();
        }

        public async Task ModifySettings(IEnumerable<int> ids)
        {
            var trainings = await trainingRepository.GetByIds(ids);

            //var ts = new TrainingSettings
            //{
            //    cultureCode = "sv-SE",
            //    alarmClockInvisible = null,
            //    customData = null,
            //    idleTimeout = null,
            //    triggers = null,
            //    manuallyUnlockedExercises = null,
            //    uniqueGroupWeights = null,
            //    trainingPlanOverrides = null,
            //    syncSettings = null,
            //    pacifistRatio = 0.1M,
            //    timeLimits = new List<decimal> { 15 }, // 33
            //};

            foreach (var training in trainings)
            {
                training.Settings.timeLimits = new List<decimal> { 20 };
                await trainingRepository.Update(training);
            }
        }

        public static async Task AddTrainingUsername(AzureTableConfig tableConfig)
        {
            var trainingRepo = new AzureTableTrainingRepository(new TypedTableClientFactory(tableConfig));
            var trainings = await trainingRepo.GetAll();
            var mnemo = new MnemoJapanese(2);
            var sinkTable = new TableClient(tableConfig.ConnectionString, $"{tableConfig.TablePrefix}");
            foreach (var training in trainings)
            {
                var username = mnemo.FromIntWithRandom(training.Id);
                var filter = $"{nameof(TableEntity.PartitionKey)} eq '{AzureTableConfig.IdToKey(training.Id)}'";
                var q = sinkTable.QueryAsync<TableEntity>(filter, 1);
                await foreach (var page in q.AsPages())
                {
                    if (page.Values.Any())
                    {
                        var value = page.Values.First();
                        var aaa = value.GetString("Data0");
                        var m = Regex.Match(aaa, "Uuid\":\"(\\w+)"); // \"Uuid\":\"gide\"
                        if (m.Success)
                        {
                            training.Username = m.Groups[1].Value;
                            await trainingRepo.Update(training);
                        }
                    }
                    break;
                }
            }
        }
    }
}
