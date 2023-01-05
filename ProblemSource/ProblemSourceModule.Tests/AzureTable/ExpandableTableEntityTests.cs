using Shouldly;
using ProblemSource.Models.Aggregates;
using Common;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using System;

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
            var phase = new Phase
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

            var converter = new ExpandableTableEntityConverter<Phase>(o => ("none", AzureTableConfig.IdToKey(o.id)));

            var tableEntity = converter.FromPoco(phase);
            // problems is too large to fit in one column - should be exploded into several
            var expandedColumns = (Dictionary<string, int>)tableEntity["__ExpandedColumns"];
            expandedColumns["problems"].ShouldBe(4);

            var recreatedPhase = converter.ToPoco(tableEntity);

            recreatedPhase.ShouldBeEquivalentTo(phase);
        }
    }
}
