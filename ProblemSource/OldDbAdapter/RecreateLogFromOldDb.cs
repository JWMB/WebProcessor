using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OldDb.Models;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;

namespace OldDbAdapter
{
    public class RecreateLogFromOldDb
    {
        public async static Task<List<LogItem>> GetAsLogItems(TrainingDbContext db, int accountId)
        {
            var phases = await GetFullPhases(db, accountId);
            return ToLogItems(phases);
        }

        public async static Task<List<Phase>> GetFullPhases(TrainingDbContext db, int accountId)
        {
            return await db.Phases
                .Include(o => o.UserTests)
                .Include(o => o.Problems)
                .ThenInclude(o => o.Answers)
                .Where(o => o.AccountId == accountId).ToListAsync();
        }

        public static List<LogItem> ToLogItems(List<Phase> phasesWithIncludes)
        {
            var result = new List<LogItem>();

            if (!phasesWithIncludes.Any())
                return result;

            // Identify and remove duplicates:
            var groupedByKey = phasesWithIncludes.GroupBy(GetRowKey).ToList();
            var withSameKey = groupedByKey.Where(o => o.Count() > 1);
            if (withSameKey.Any())
            {
                var otherProps = withSameKey.Select(o => new { o.Key, Values = o.Select(p => 
                    new { p.Sequence, p.PhaseType, UserTestCount = p.UserTests.Count, ProblemCount = p.Problems.Count(), AnswerCount = p.Problems.Sum(q => q.Answers.Count) }).Distinct().ToList() });
                var withDiffering = otherProps.Where(o => o.Values.Count > 1);
                if (withDiffering.Any())
                {
                    //throw new Exception($"Same key but different items: {System.Text.Json.JsonSerializer.Serialize(withDiffering)}");
                }
                foreach (var grp in withSameKey)
                {
                    var duplicates = phasesWithIncludes.Where(o => GetRowKey(o) == grp.Key).ToList();
                    foreach (var item in duplicates.OrderByDescending(o => o.UserTests.Count + o.Problems.Sum(q => q.Answers.Count)).Skip(1))
                        phasesWithIncludes.Remove(item);
                }
            }

            foreach (var p in phasesWithIncludes.Where(o => o.Exercise?.Contains("N/A") == true))
                p.Exercise = "undef";

            var ordered = phasesWithIncludes.OrderBy(o => o.TrainingDay).ThenBy(o => o.Time);
            var previous = ordered.First();
            foreach (var phase in ordered)
            {
                if (phase.TrainingDay > previous.TrainingDay)
                {
                    // TODO: construct UserStatePushLogItem:s..?
                    result.Add(CreateLogItem(previous.Time, new EndOfDayLogItem { training_day = previous.TrainingDay }));
                }

                result.AddRange(PhaseToLogItems(phase));

                previous = phase;
            }
            return result;

            string GetRowKey(Phase p) => $"{p.TrainingDay}_{(p.Exercise ?? "").Replace("#", "")}_{p.Time}"; // ProblemSource.Models.Aggregates.Phase.UniqueIdWithinUser()
        }

        public static List<LogItem> PhaseToLogItems(Phase phase)
        {
            var result = new List<LogItem>();

            result.Add(CreateLogItem(phase.Time, new NewPhaseLogItem
            {
                exercise = phase.Exercise ?? "",
                phase_type = phase.PhaseType ?? "",
                sequence = phase.Sequence ?? 0,
                training_day = phase.TrainingDay,
            }));

            foreach (var prob in phase.Problems)
            {
                result.Add(CreateLogItem(prob.Time, new NewProblemLogItem
                {
                    level = prob.Level,
                    problem_string = prob.ProblemString ?? "",
                    problem_type = prob.ProblemType ?? "",
                }));

                foreach (var answ in prob.Answers)
                {
                    result.Add(CreateLogItem(answ.Time, new AnswerLogItem
                    {
                        answer = answ.Answer1 ?? "",
                        correct = answ.Correct,
                        //correctAnswer = "",
                        //errorType = "",
                        group = answ.Group,
                        response_time = answ.ResponseTime,
                        tries = answ.Tries,
                    }));
                }
            }

            var phaseEnd = CreateLogItem(phase.Time, new PhaseEndLogItem());
            if (phase.UserTests.Any())
            {
                var ut = phase.UserTests.Single();
                phaseEnd.noOfCorrect = ut.Corrects;
                phaseEnd.noOfIncorrect = ut.Incorrects;
                phaseEnd.noOfQuestions = ut.Questions;
                phaseEnd.completedPlanet = ut.CompletedPlanet;
                phaseEnd.planetTargetScore = ut.PlanetTargetScore;
                phaseEnd.score = ut.Score;
                phaseEnd.targetScore = ut.TargetScore;
                phaseEnd.wonRace = ut.WonRace;
            }
            result.Add(phaseEnd);

            return result;
        }

        private static TLogItem CreateLogItem<TLogItem>(long time, TLogItem? init = null) where TLogItem : LogItem, new()
        {
            if (init == null)
                init = new TLogItem();
            init.time = time;
            init.className = typeof(TLogItem).Name;
            return init;
        }
    }
}
