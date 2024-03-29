﻿using Azure.Data.Tables;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource;
using ProblemSourceModule.Services.Storage.AzureTables;
using System.Text.RegularExpressions;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Models;

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

        public static List<(string Name, int Id)> ExtractTrainingNames(string input)
        {
            var names = Regex.Matches(input, @"\w{4} \w{4,8}").OfType<Match>().Select(o => o.Value).ToList();

            var mnemoJapanese = new MnemoJapanese(2);
            var hashedUsername = new UsernameHashing(mnemoJapanese, 2);
                
            var withIds = names.Select(o => new { Name = o, Id = mnemoJapanese.ToIntWithRandom(hashedUsername.Dehash(o)!) } ).ToList();
            if (withIds.Any(o => o.Id == null))
                throw new Exception($"Couldn't parse {string.Join(",", withIds.Where(o => o.Id == null).Select(o => o.Name))}");
            return withIds.Select(o => (o.Name, o.Id!.Value)).ToList();
        }

        public async Task<List<int>> MoveTeachersTrainingsToGroup(string userEmail, IEnumerable<int> ids, string toGroup, bool actuallyMove = false)
        {
            var user = await userRepository.Get(userEmail);
            if (user == null) throw new Exception($"User not found");

            var available = user.Trainings.SelectMany(o => o.Value).Intersect(ids);
            var tmp = user.Trainings.ToDictionary(o => o.Key, o => o.Value.Except(available));

            if (!tmp.ContainsKey(toGroup))
                tmp[toGroup] = new List<int>();
            tmp[toGroup] = tmp[toGroup].Concat(available).Distinct();

            user.Trainings = new UserTrainingsCollection(tmp);

            if (actuallyMove)
                await userRepository.Update(user);

            return available.ToList();
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

        public async Task CopyTrainingTemplate(IEnumerable<int> ids, Training template)
        {
            var trainings = await trainingRepository.GetByIds(ids);
            foreach (var training in trainings)
            {
                // TODO: could we e.g. store the template that was used, retrieve it here, and keep any modifications that were done compared to that template?
                training.TrainingPlanName = template.TrainingPlanName;
                training.Settings.Analyzers = template.Settings.Analyzers;
                await trainingRepository.Update(training);
            }
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
