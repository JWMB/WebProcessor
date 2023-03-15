using ProblemSource.Models.LogItems;
using System.Text.RegularExpressions;

namespace ProblemSource.Models.Aggregates
{
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

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is Phase typed == false)
                return false;
            return training_day == typed.training_day && exercise == typed.exercise && phase_type == typed.phase_type && time == typed.time && problems.Count() == typed.problems.Count();
        }


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
                exercise = "undef",
                time = time,
                phase_type = "undef",
                training_day = trainingDay,
            };
        }

        public static string GetExerciseCommonName(string exercise) => Regex.Replace(exercise, @"#\d+", ""); // TODO: #intro as well?

        public static string UniqueIdWithinUser(Phase p) => $"{p.training_day}_{p.exercise.Replace("#", "")}_{p.time}";

        public static Phase CreateForTest(int suffix)
        {
            return new Phase
            {
                exercise = $"a{suffix}",
                phase_type = "a",
                training_day = 1,
                sequence = 1,
                time = 1,
                user_test = new UserTest
                {
                },
                problems = 
                    Enumerable.Range(0, 10).Select(pi => new Problem
                    {
                        answers = Enumerable.Range(0, 10).Select(ai =>
                            new Answer
                            {
                                answer = "a",
                                correct = true,
                                response_time = 1,
                                time = 1,
                                tries = 1
                            }
                        ).ToList()
                    }).ToList(),
            };
        }
    }
}
