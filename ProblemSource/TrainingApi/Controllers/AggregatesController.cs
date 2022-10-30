using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AggregatesController : ControllerBase
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory;
        private readonly OldDbRaw oldDb;
        private readonly ILogger<AggregatesController> _logger;

        public AggregatesController(IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory, OldDbRaw oldDb, ILogger<AggregatesController> logger)
        {
            this.userGeneratedDataRepositoryProviderFactory = userGeneratedDataRepositoryProviderFactory;
            this.oldDb = oldDb;
            _logger = logger;
        }

        IUserGeneratedDataRepositoryProvider GetDataProvider(int accountId) =>
            userGeneratedDataRepositoryProviderFactory.Create("7af76409-a7ee-44e9-9b4a-526b164de5f0");

        [HttpGet]
        [Route("TrainingDayAccount")]
        public async Task<IEnumerable<TrainingDayAccount>> TrainingDayAccount(int accountId)
        {
            // id	uuid	trainingDay	startTime	endTimeStamp	numRacesWon	numRaces	numPlanetsWon	numCorrectAnswers	numQuestions	responseMinutes	remainingMinutes

            var result = await oldDb.Read($"SELECT data FROM aggregated_data WHERE aggregator_id = 2 AND account_id = {accountId}", 
                (reader, columns) => {
                    var data = reader.GetString(0).Split('\t');
                    return new TrainingDayAccount
                    {
                        AccountId = int.Parse(data[0]),
                        AccountUuid = data[1],
                        TrainingDay = int.Parse(data[2]),
                        StartTime = DateTime.UtcNow, //data[3]
                        EndTimeStamp = DateTime.UtcNow, // data[4]
                        NumRacesWon = int.Parse(data[5]),
                        NumRaces = int.Parse(data[6]),
                        NumPlanetsWon = int.Parse(data[7]),
                        NumCorrectAnswers = int.Parse(data[8]),
                        ResponseMinutes = int.Parse(data[9]),
                        RemainingMinutes = int.Parse(data[10]),
                    };
                });
            return result;
            //return await GetDataProvider(accountId).TrainingDays.GetAll();
        }

        [HttpGet]
        [Route("PhaseStatistics")]
        public async Task<IEnumerable<PhaseStatistics>> PhaseStatistics(int accountId)
        {
            return await GetDataProvider(accountId).PhaseStatistics.GetAll();
        }
    }
}