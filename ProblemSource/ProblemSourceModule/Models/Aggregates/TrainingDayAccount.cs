using Common;
using ProblemSource.Models.LogItems;
using System.Data;
using System.Text.RegularExpressions;

namespace ProblemSource.Models.Aggregates
{
    public class TrainingDayAccount
    {
        public int AccountId { get; set; }
        public string AccountUuid { get; set; } = string.Empty;
        public int TrainingDay { get; set; }
        public DateTime StartTime { get; set; }

        public DateTime EndTimeStamp { get; set; }
        public int NumRacesWon { get; set; }
        public int NumRaces { get; set; }
        public int NumPlanetsWon { get; set; }
        public int NumCorrectAnswers { get; set; }
        public int NumQuestions { get; set; }
        public int ResponseMinutes { get; set; }
        public int RemainingMinutes { get; set; }

        public override string ToString()
        {
            // '{AccountUuid}' 
            return $"{TrainingDay} {StartTime} rTime:{ResponseMinutes} #corr:{NumCorrectAnswers} #q:{NumQuestions} #racewon:{NumRacesWon}";
        }

        public static List<TrainingDayAccount> Create(string uuid, int accountId, IEnumerable<Phase> multiDayPhases)
        {
            return multiDayPhases.GroupBy(o => o.training_day).Select(dayAndPhases =>
            {
                var phases = dayAndPhases.ToList();

                var userTests = phases.Where(_ => _.user_test != null).Select(_ => _.user_test).OfType<UserTest>().ToList(); //TODO: also where _.ended ?

                var allAnswers = phases.SelectMany(_ => _.problems.SelectMany(p => p.answers));
                var allLastAnswers = phases.SelectMany(o => o.problems.Select(p => p.answers.OrderBy(a => a.time).LastOrDefault())).OfType<Answer>();
                int responseTime;
                try
                {
                    responseTime = allLastAnswers.Sum(_ => _.response_time);
                }
                catch (OverflowException)
                {
                    responseTime = int.MaxValue;
                }

                var phasesOrdered = phases.OrderBy(_ => _.time).ToList();
                var lastPhase = phasesOrdered.Last();
                var lastTime = lastPhase.time;
                if (lastPhase.problems.Count > 0)
                {
                    var lastProb = lastPhase.problems.OrderBy(_ => _.time).Last();
                    lastTime = lastProb.time;
                    if (lastProb.answers.Count > 0)
                    {
                        lastTime = lastProb.answers.OrderBy(_ => _.time).Last().time;
                    }
                }
                var totalTime = lastTime - phasesOrdered.First().time;

                return new TrainingDayAccount
                {
                    AccountId = accountId,
                    AccountUuid = uuid,
                    TrainingDay = dayAndPhases.Key,
                    StartTime = new DateTime(1970, 1, 1).AddMilliseconds(phases.Any() ? phases.Min(o => o.time) : 0),
                    EndTimeStamp = new DateTime(1970, 1, 1).AddMilliseconds(lastTime),
                    NumRacesWon = userTests.Where(o => o.won_race).Count(),
                    NumRaces = userTests.Count(),
                    NumPlanetsWon = userTests.Where(o => o.completed_planet).Count(),
                    NumCorrectAnswers = allAnswers.Count(o => o.correct),
                    NumQuestions = phases.Sum(o => o.problems.Count),
                    ResponseMinutes = responseTime / 1000 / 60,
                    RemainingMinutes = (int)((totalTime - (long)responseTime) / 1000 / 60)
                };
            }).ToList();
        }

        public static List<TrainingDayAccount> Create(string uuid, int accountId, IEnumerable<PhaseStatistics> multiDayPhases)
        {
            return multiDayPhases.GroupBy(o => o.training_day).Select(dayAndPhases =>
            {
                var phases = dayAndPhases.ToList();
                int responseTime;
                try
                {
                    responseTime = phases.Sum(o => o.response_time_total);
                }
                catch (OverflowException)
                {
                    responseTime = int.MaxValue;
                }

                var result = new TrainingDayAccount
                {
                    AccountId = accountId,
                    AccountUuid = uuid,
                    TrainingDay = dayAndPhases.Key,
                    StartTime = phases.MinOrDefault(o => o.timestamp, new DateTime(1970, 1, 1)),
                    EndTimeStamp = phases.OrderBy(o => o.timestamp).LastOrDefault()?.end_timestamp ?? new DateTime(1970, 1, 1),
                    NumRacesWon = phases.Count(o => o.won_race == true),
                    NumRaces = phases.Count(o => o.won_race != null),
                    NumPlanetsWon = phases.Count(o => o.completed_planet == true),
                    NumCorrectAnswers = phases.SumOrDefault(o => o.num_correct_answers),
                    NumQuestions = phases.SumOrDefault(o => o.num_questions),
                    ResponseMinutes = responseTime / 1000 / 60,
                };
                result.RemainingMinutes = (int)(result.EndTimeStamp - result.StartTime).TotalMinutes - result.ResponseMinutes;
                return result;
            }).ToList();
        }
    }

    public class Account
    {
        public int id { get; set; }
        public string uuid { get; set; } = string.Empty;
        public int person_id { get; set; }
        public int training_plan_id { get; set; }
        public DateTime created_at { get; set; }
        public int secondary_training_plan_id { get; set; }
        public List<Phase> phases { get; set; } = new();

        public List<Group> groups { get; set; } = new();
    }

    public class Phase
    {
        //public string uuid { get; set; }
        public int id { get; set; }
        public int training_day { get; set; }
        public string exercise { get; set; } = string.Empty;
        public string phase_type { get; set; } = string.Empty;
        public long time { get; set; }
        public int sequence { get; set; }
        public List<Problem> problems { get; set; } = new(); 
        public UserTest? user_test { get; set; }

        public static Phase Create(NewPhaseLogItem newPhase) //, string userId)
        {
            return new Phase
            {
                //uuid = userId,
                time = newPhase.time,
                phase_type = newPhase.phase_type,
                exercise = newPhase.exercise,
                training_day = newPhase.training_day,
                sequence = newPhase.sequence,
            };
        }

        public static Phase CreateUnknown(long time, int trainingDay)
        {
            return new Phase
            {
                exercise = "N/A",
                time = time,
                phase_type = "N/A",
                training_day = trainingDay,
            };
        }

        public static string UniqueIdWithinUser(Phase p) => $"{p.training_day}_{p.exercise}_{p.time}";
    }

    public class Problem
    {
        public int id { get; set; }
        public int phase_id { get; set; }
        public decimal level { get; set; }

        public long time { get; set; }
        public string problem_type { get; set; } = string.Empty;
        public string problem_string { get; set; } = string.Empty;

        public List<Answer> answers { get; set; } = new();

        public static Problem Create(NewProblemLogItem newProblem)
        {
            return new Problem
            {
                level = newProblem.level,
                problem_string = newProblem.problem_string,
                problem_type = newProblem.problem_type,
                time = newProblem.time,
            };
        }

        public static Problem CreateUnknown(long time)
        {
            return new Problem
            {
                level = 0,
                problem_string = "N/A",
                problem_type = "N/A",
                time = time,
            };
        }
    }

    public class Answer
    {
        public int id { get; set; }
        public int problem_id { get; set; }
        public long time { get; set; }
        public bool correct { get; set; }
        public int response_time { get; set; }
        public string answer { get; set; } = string.Empty;
        public int tries { get; set; }

        public static Answer Create(AnswerLogItem answer)
        {
            return new Answer
            {
                answer = answer.answer,
                correct = answer.correct,
                response_time = answer.response_time,
                time = answer.time,
                tries = answer.tries
            };
        }
    }

    public class UserTest
    {
        public int score { get; set; }
        public int target_score { get; set; }
        public int planet_target_score { get; set; }
        public bool won_race { get; set; }
        public bool completed_planet { get; set; }
        public bool? ended { get; set; }

        public static UserTest Create(PhaseEndLogItem phaseEnd)
        {
            return new UserTest
            {
                completed_planet = phaseEnd.completedPlanet,
                planet_target_score = (int)phaseEnd.planetTargetScore,
                score = (int)phaseEnd.score,
                target_score = (int)phaseEnd.targetScore,
                won_race = phaseEnd.wonRace,
                ended = true // TODO: ?
            };
        }
    }
}
