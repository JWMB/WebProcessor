using Azure.Data.Tables;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource;
using ProblemSourceModule.Services.Storage.AzureTables;
using System.Text.RegularExpressions;
using ProblemSource.Models;

namespace Tools
{
    internal class TrainingMod
    {
        public static async Task ModifySettings(AzureTableConfig tableConfig)
        {
            var trainingRepo = new AzureTableTrainingRepository(new TypedTableClientFactory(tableConfig));
            var ids = Enumerable.Range(32, 30);
            var trainings = await trainingRepo.GetByIds(ids);

            var ts = new TrainingSettings
            {
                cultureCode = "sv-SE",
                alarmClockInvisible = null,
                customData = null,
                idleTimeout = null,
                triggers = null,
                manuallyUnlockedExercises = null,
                uniqueGroupWeights = null,
                trainingPlanOverrides = null,
                syncSettings = null,
                pacifistRatio = 0.1M,
                timeLimits = new List<decimal> { 15 }, // 33
            };

            foreach (var training in trainings)
            {
                training.Settings = ts;
                await trainingRepo.Update(training);
            }

            await Task.Delay(1);
            return;
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
