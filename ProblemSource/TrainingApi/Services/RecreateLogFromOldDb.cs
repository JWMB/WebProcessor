using Microsoft.EntityFrameworkCore;
using OldDb.Models;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;

namespace TrainingApi.Services
{
    public class RecreateLogFromOldDb
    {
        public async static Task<List<Phase>> Get(TrainingDbContext db, int accountId)
        {
            return await db.Phases
                .Include(o => o.UserTests)
                .Include(o => o.Problems)
                .ThenInclude(o => o.Answers)
                .Where(o => o.AccountId == accountId).ToListAsync();
        }

        public static List<LogItem> ToLogItems(IEnumerable<Phase> phasesWithIncludes)
        {
            
            var result = new List<LogItem>();
            foreach (var phase in phasesWithIncludes.OrderBy(o => o.Time))
            {
                result.AddRange(PhaseToLogItems(phase));
            }
            return result;
        }

        public static List<LogItem> PhaseToLogItems(Phase phase)
        {
            var result = new List<LogItem>();

            result.Add(CreateLogItem(phase.Time, new NewPhaseLogItem {
                exercise = phase.Exercise ?? "",
                phase_type = phase.PhaseType ?? "",
                sequence = phase.Sequence ?? 0,
                training_day = phase.TrainingDay,
            }));

            foreach (var prob in phase.Problems)
            {
                result.Add(CreateLogItem(prob.Time, new NewProblemLogItem {
                    level = prob.Level,
                    problem_string = prob.ProblemString ?? "",
                    problem_type = prob.ProblemType ?? "" ,
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
