using Shouldly;
using ProblemSource.Models.Aggregates;
using Common;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSource.Services.Storage.AzureTables;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class ExpandableTableEntityTests
    {
        [Fact]
        public void StringSplit_Test()
        {
            var longString = "0123456789012345678901234567890123456789";
            var split = longString.SplitByLength(10);
            string.Join("", split).Length.ShouldBe(longString.Length);
        }

        [Fact]
        public void ExpandableTableEntity_Conversion()
        {
            var phase = CreateLargePhase();

            var converter = new ExpandableTableEntityConverter<Phase>(o => new TableFilter("none", AzureTableConfig.IdToKey(o.id)));

            var tableEntity = converter.FromPoco(phase);
            // problems is too large to fit in one column - should be exploded into several
            var expandedColumns = JsonConvert.DeserializeObject<Dictionary<string, int>>(tableEntity.GetString("__ExpandedColumns"));
            expandedColumns["problems"].ShouldBe(4);

            var recreatedPhase = converter.ToPoco(tableEntity);

            recreatedPhase.ShouldBeEquivalentTo(phase);
        }

        private Phase CreateLargePhase()
        {
            return new Phase
            {
                exercise = "exercise",
                problems = Enumerable.Range(0, 100)
                    .Select(pi => new Problem
                    {
                        problem_string = $"problem",
                        answers = Enumerable.Range(0, 10)
                            .Select(o => new Answer
                            {
                            }).ToList()
                    }).ToList()
            };
        }

        [SkippableFact]
        public async Task ExpandableTableEntity_ExpandedInAzureTable()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);

            var converter = new ExpandableTableEntityConverter<Phase>(o => new TableFilter("none", AzureTableConfig.IdToKey(o.id)));
            var item = CreateLargePhase();
            var tableEntity = converter.FromPoco(item);

            var clientFactory = AzureTableTestBase.CreateTypedTableClientFactory();
            var client = await clientFactory.CreateClientAndInit("expandabletest");
            await client.UpsertEntityAsync(tableEntity, Azure.Data.Tables.TableUpdateMode.Replace);

            var retrieved = await client.GetEntityAsync<TableEntity>(tableEntity.PartitionKey, tableEntity.RowKey);
            var converted = converter.ToPoco(retrieved);
            converted.problems.Count().ShouldBe(item.problems.Count);
        }
    }
}
