using Common;
using System.Data;

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

        public static List<TrainingDayAccount> Create(int accountId, IEnumerable<Phase> multiDayPhases)
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
                    AccountUuid = "",
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

        public static List<TrainingDayAccount> Create(int accountId, IEnumerable<PhaseStatistics> multiDayPhases)
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
                    AccountUuid = "",
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

    //public class Account
    //{
    //    public int id { get; set; }
    //    public string uuid { get; set; } = string.Empty;
    //    public int person_id { get; set; }
    //    public int training_plan_id { get; set; }
    //    public DateTime created_at { get; set; }
    //    public int secondary_training_plan_id { get; set; }
    //    public List<Phase> phases { get; set; } = new();

    //    public List<Group> groups { get; set; } = new();
    //}
}
