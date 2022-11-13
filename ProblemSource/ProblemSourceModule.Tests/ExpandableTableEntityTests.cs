using AutoFixture;
using Newtonsoft.Json;
using PluginModuleBase;
using ProblemSource.Models.LogItems;
using ProblemSource.Models;
using ProblemSource.Services;
using AutoFixture.AutoMoq;
using Shouldly;
using ProblemSource.Services.Storage;
using Moq;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using Common;

namespace ProblemSourceModule.Tests
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
            
            //var tableEntity = ExpandableTableEntityConverter<Phase>.FromPoco(phase);
            //var recreatedPhase = ExpandableTableEntityConverter<Phase>.ToPoco(tableEntity);

            //recreatedPhase.ShouldBeEquivalentTo(phase);
        }
    }
}
