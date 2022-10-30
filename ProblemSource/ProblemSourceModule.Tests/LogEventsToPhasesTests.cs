using ProblemSource.Models.LogItems;
using ProblemSource.Models;
using ProblemSource.Services;
using Shouldly;

namespace ProblemSource.Tests
{
    public class LogEventsToPhasesTests
    {
        [Fact]
        public void LogEventsToPhases_AlreadySynced()
        {
            // Arrange
            var logItems = new List<LogItem>
            {
                new NewPhaseLogItem { time = 1, exercise = "A" },
                new NewProblemLogItem { time = 2 },
                new AnswerLogItem { time = 3 }
            }.Prepare();

            var phasesResult = LogEventsToPhases.Create(logItems);

            // Pre-assert
            phasesResult.PhasesCreated.Count.ShouldBe(1);
            phasesResult.Errors.ShouldBeEmpty();

            logItems = new List<LogItem> {
                new SyncLogStateLogItem { type = "ALREADY_SYNCED" },
            }.Concat(logItems)
            .Concat(new List<LogItem> {
                new SyncLogStateLogItem { type = "NOT_SYNCED" },
                new NewProblemLogItem { time = 6 },
                new AnswerLogItem { time = 7 },
                new PhaseEndLogItem { time = 8 },
                new NewPhaseLogItem { time = 9, exercise = "B" },
                new NewProblemLogItem { time = 10 },
                new AnswerLogItem { time = 11 },
                new PhaseEndLogItem { time = 12 },
            }).ToList();

            // Act
            var newPhasesResult = LogEventsToPhases.Create(logItems, phasesResult.PhasesCreated);

            // Assert
            newPhasesResult.PhasesUpdated.Count.ShouldBe(1);
            newPhasesResult.PhasesCreated.Count.ShouldBe(1);
        }
    }
}
