using Microsoft.EntityFrameworkCore;
using OldDb.Models;
using ProblemSource;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using TrainingApi.Controllers;
using Phase = ProblemSource.Models.Aggregates.Phase;

namespace TrainingApi.Services
{
    public interface IStatisticsProvider
    {
        Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int accountId);
        Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int accountId);
    }

    public class OldDbStatisticsProvider : IStatisticsProvider
    {
        private readonly TrainingDbContext dbContext;

        public OldDbStatisticsProvider(TrainingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int accountId)
        {
            var rows = await dbContext.AggregatedData.Where(o => o.AggregatorId == 1 && o.AccountId == accountId)
                .ToListAsync();
            return rows.Select(o => o.ToPhaseStatistics()).OfType<PhaseStatistics>();
        }

        public async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int accountId)
        {
            var rows = await dbContext.AggregatedData.Where(o => o.AggregatorId == 2 && o.AccountId == accountId)
                .ToListAsync();
            return rows.Select(o => o.ToTyped()).OfType<TrainingDayAccount>();
        }
    }

    public class StatisticsProvider : IStatisticsProvider
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory;
        private readonly MnemoJapanese mnemoJapanese;
        private readonly IUserStateRepository userStateRepository;

        public StatisticsProvider(IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory, MnemoJapanese mnemoJapanese, IUserStateRepository userStateRepository)
        {
            this.userGeneratedDataRepositoryProviderFactory = userGeneratedDataRepositoryProviderFactory;
            this.mnemoJapanese = mnemoJapanese;
            this.userStateRepository = userStateRepository;
        }

        // TODO: we should use int accountId as rowKey...
        private static Dictionary<int, string> idToHashed = new Dictionary<int, string>();

        private IUserGeneratedDataRepositoryProvider GetDataProvider(int accountId)
        {
            // TODO: we should use int accountId as rowKey
            var uuid = "";
            if (userStateRepository is AzureTableUserStateRepository azureTableStateRepo)
            {
                if (!idToHashed.TryGetValue(accountId, out uuid))
                {
                    var uuids = azureTableStateRepo.GetUuids().Result;
                    idToHashed = uuids.Select(o => new { Key = o, Value = mnemoJapanese.ToIntWithRandom(o) })
                        .Where(o => o.Value.HasValue)
                        .ToDictionary(o => o.Value ?? 0, o => o.Key);
                    uuid = idToHashed.GetValueOrDefault(accountId, "");
                }
            }
            return userGeneratedDataRepositoryProviderFactory.Create(uuid);
        }

        public async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int accountId) =>
            await GetDataProvider(accountId).PhaseStatistics.GetAll();

        public async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int accountId) =>
            await GetDataProvider(accountId).TrainingDays.GetAll();
    }

    public class RecreatedStatisticsProvider : IStatisticsProvider
    {
        private readonly TrainingDbContext dbContext;

        public RecreatedStatisticsProvider(TrainingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int accountId) => PhaseStatistics.Create(accountId, await RecreatePhases(accountId));

        public async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int accountId) => TrainingDayAccount.Create("", 0, await RecreatePhases(accountId));

        private async Task<List<Phase>> RecreatePhases(int accountId)
        {
            var phases = await RecreateLogFromOldDb.GetFullPhases(dbContext, accountId);
            var log = RecreateLogFromOldDb.ToLogItems(phases);
            return LogEventsToPhases.Create(log, null).PhasesCreated;
        }
    }

    public static class OldAggregatesExtensions
    {
        public static PhaseStatistics? ToPhaseStatistics(this AggregatedDatum? row)
        {
            if (row?.Data == null)
                return null;
            var data = row.Data.Split('\t');
            // Id	Account	Training Day	Exercise	Phase-id	PhaseType	Timestamp	Sequence	No of questions	No of Correct answers	No of Incorrect answers	No of correct on first try	% correct on first try	Lowest Level	Highest level	Average respone time	Total response time	ERR #removed dup answ	ERR #prob w/o answ	Score	Target score	Planet target score	Won race	Completed planet
            var result = new PhaseStatistics
            {
                account_id = int.Parse(data[0]),
                //ac = data[1],
                training_day = int.Parse(data[2]),
                exercise = data[3],
                //phase_id = data[4],
                phase_type = data[5],
                timestamp = DateTime.Parse(data[6]), // new DateTime(1970, 1, 1).AddMilliseconds(long.Parse(data[6])),
                sequence = int.Parse(data[7]),
                num_questions = int.Parse(data[8]),
                num_correct_answers = int.Parse(data[9]),
                num_incorrect_answers = int.Parse(data[10]),
                num_correct_first_try = int.Parse(data[11]),
                //percent
                level_min = data[13] == "" ? 0M : decimal.Parse(data[13], System.Globalization.CultureInfo.InvariantCulture),
                level_max = data[14] == "" ? 0M : decimal.Parse(data[14], System.Globalization.CultureInfo.InvariantCulture),
                response_time_avg = int.Parse(data[15]),
                response_time_total = data[16] == "" ? 0 : int.Parse(data[16]),
            };

            if (data.Length > 17)
            {
                //	ERR #removed dup answ
                //	ERR #prob w/o answ	
                //	Score	Target score	
                //	Planet target score
                if (data.Length > 19)
                {
                    result.won_race = bool.Parse(data[22]);
                    result.completed_planet = bool.Parse(data[23]);
                }
            }
            return result;
        }

        public static TrainingDayAccount? ToTyped(this AggregatedDatum? row)
        {
            if (row?.Data == null)
                return null;
            var data = row.Data.Split('\t');
            // id	uuid	trainingDay	startTime	endTimeStamp	numRacesWon	numRaces	numPlanetsWon	numCorrectAnswers	numQuestions	responseMinutes	remainingMinutes
            return new TrainingDayAccount
            {
                AccountId = int.Parse(data[0]),
                AccountUuid = data[1],
                TrainingDay = int.Parse(data[2]),
                StartTime = DateTime.Parse(data[3]), //data[3]
                EndTimeStamp = new DateTime(1970, 1, 1).AddMilliseconds(long.Parse(data[4])),
                NumRacesWon = int.Parse(data[5]),
                NumRaces = int.Parse(data[6]),
                NumPlanetsWon = int.Parse(data[7]),
                NumCorrectAnswers = int.Parse(data[8]),
                NumQuestions = int.Parse(data[9]),
                ResponseMinutes = int.Parse(data[10]),
                RemainingMinutes = int.Parse(data[11]),
            };
        }
    }
}
