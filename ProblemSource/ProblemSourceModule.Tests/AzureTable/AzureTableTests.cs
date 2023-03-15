using Azure;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Models.Aggregates;
using Shouldly;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureTableTests : AzureTableTestBase
    {   
        [SkippableFact]
        public async Task InvalidAzureKeyCharactersHandled()
        {
            await Init();

            var userId = 1;
            var phases = new[] {
                new Phase { exercise = "a#" },
                //new Phase { exercise = "a?" } // TODO
            };

            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, userId);

            await Should.NotThrowAsync(async () => await userRepos.Phases.Upsert(phases));
        }

        [SkippableTheory]
        [InlineData("0001-01-01T00:00:00+00:00", true)]
        [InlineData("1601-01-01T00:00:00+00:00", false)]
        public async Task InvalidDateTimeOffset(string dateTimeString, bool expectFailure)
        {
            await Init();

            var userId = 1;
            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, userId);

            var dateTime = DateTimeOffset.Parse(dateTimeString);

            var action = async () => await userRepos.TrainingSummaries.Upsert(new[] { new TrainingSummary { FirstLogin = dateTime, LastLogin = dateTime } });
            if (expectFailure)
                await Should.ThrowAsync(action(), typeof(Exception));
            else
                await Should.NotThrowAsync(action());
        }

        [SkippableFact]
        public async Task TableClient_StorePhase()
        {
            await Init();

            var userId = 1;
            //var phaseData = """{ "id":0,"training_day":3,"exercise":"tangram01#0","phase_type":"TEST","time":1666182070947,"sequence":0,"problems":[{ "id":0,"phase_id":0,"level":1.5,"time":1666182072961,"problem_type":"ProblemTangram","problem_string":"triangles","answers":[]}],"user_test":{ "score":0,"target_score":3,"planet_target_score":3,"won_race":false,"completed_planet":false,"ended":true}}""";
            //var phase = JsonConvert.DeserializeObject<Phase>(phaseData);
            //if (phase == null)
            //    throw new NullException("Deserializing phaseData");
            var phase = new Phase { id = 0, training_day = 3, exercise = "tangram01", phase_type = "TEST", time = 1666182070947, sequence = 0, problems = new List<Problem>(), user_test = new UserTest() };

            var tableEntity = PhaseTableEntity.FromBusinessObject(phase!, userId);
            try
            {
                var response = await tableClientFactory.Phases.UpsertEntityAsync(tableEntity);
                response.Status.ShouldBe(204); //409 (Conflict)
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new Exception("Running old version of Azurite? This was fixed in 3.19.0 https://github.com/Azure/Azurite/issues/1565");
            }

            //var repo = new TableEntityRepository<Phase, PhaseTableEntity>(clientFactory.Phases, p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), userId);
            //await repo.AddOrUpdate(new[] { phase });
        }
    }
}
