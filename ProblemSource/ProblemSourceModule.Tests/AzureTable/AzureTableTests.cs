using AutoFixture;
using AutoFixture.AutoMoq;
using Azure;
using Microsoft.Extensions.Logging;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            await Should.NotThrowAsync(async () => await userRepos.Phases.AddOrUpdate(phases));
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
