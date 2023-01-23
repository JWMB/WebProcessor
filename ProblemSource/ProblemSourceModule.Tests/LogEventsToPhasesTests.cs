using ProblemSource.Models.LogItems;
using ProblemSource.Models;
using ProblemSource.Services;
using Shouldly;

namespace ProblemSource.Tests
{
    public class LogEventsToPhasesTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LogEventsToPhases_AlreadySynced(bool sameTimeStamps)
        {
            // Arrange
            var logItems = new List<LogItem>
            {
                new NewPhaseLogItem { exercise = "A" },
                new NewProblemLogItem { },
                new AnswerLogItem {  }
            }.Prepare(1);

            var phasesResult = LogEventsToPhases.Create(logItems);

            // Pre-assert
            phasesResult.PhasesCreated.Count.ShouldBe(1);
            phasesResult.Errors.ShouldBeEmpty();

            logItems = new List<LogItem> {
                new SyncLogStateLogItem { type = "ALREADY_SYNCED" },
            }.Concat(logItems)
            .Concat(new List<LogItem> {
                new SyncLogStateLogItem { type = "NOT_SYNCED" },
                new NewProblemLogItem { },
                new AnswerLogItem { },
                new PhaseEndLogItem { },
                new NewPhaseLogItem { exercise = "B" },
                new NewProblemLogItem { },
                new AnswerLogItem {  },
                new PhaseEndLogItem {  },
            })
            .Prepare(1, overrideExistingTime: sameTimeStamps == false).ToList();

            // Act
            var newPhasesResult = LogEventsToPhases.Create(logItems, phasesResult.PhasesCreated);

            // Assert
            if (sameTimeStamps)
            {
                newPhasesResult.Errors.ShouldBeEmpty();
                newPhasesResult.PhasesUpdated.Count.ShouldBe(1);
                newPhasesResult.PhasesCreated.Count.ShouldBe(1);
            }
            else
            {
                newPhasesResult.Errors.ShouldContain("Could not find parent of NewPhaseLogItem (time=2)");
            }
        }

        [Fact]
        public void LogEventsToPhases_SameDayTwice()
        {
            // E.g. no sync after training on device A, switch to device B and "redo" the same training day, sync, then switch to A and sync

            var mocker = new LogItemsMocker();
            var numPhases = 5;

            var deviceBResult = LogEventsToPhases.Create(mocker.CreateDay(1, numPhases).Prepare(10000));
            deviceBResult.PhasesCreated.Count.ShouldBe(numPhases);

            var deviceAResult = LogEventsToPhases.Create(mocker.CreateDay(1, numPhases).Prepare(1), deviceBResult.PhasesCreated);
            // The new phases are also created (even though we'll get double data for the day)
            deviceAResult.PhasesCreated.Count.ShouldBe(numPhases);
        }
    }

    public class LogItemsMocker
    {
        public List<LogItem> CreateDay(int trainingDay, int numExercises = 5, int numProblemsPerExercise = 5)
        {
            return Enumerable.Range(0, numExercises).Select((o, i) => CreatePhase(trainingDay, $"ex_{i}", numProblemsPerExercise)).SelectMany(o => o)
                .ToList();
        }

        public List<LogItem> CreatePhase(int trainingDay, string exercise, int numProblems = 5)
        {
            return new List<LogItem>
            {
                new NewPhaseLogItem { training_day = trainingDay, exercise = exercise },
            }.Concat(
                Enumerable.Range(0, numProblems).Select(o => CreateProblem()).SelectMany(o => o)
            ).ToList();
        }

        public List<LogItem> CreateProblem(decimal level = 1, bool correct = true, int responseTime = 1000)
        {
            return new List<LogItem>
            {
                new NewProblemLogItem { level = level },
                new AnswerLogItem { correct = correct, response_time = responseTime },
            };
        }
    }
}
