using Azure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProblemSourceModule.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProblemSource.Models.Aggregates
{
    public class MLFeaturesJulia
    {
        public static MLFeaturesJulia FromPhases(IEnumerable<Phase> phases, int dayCutoff = 5)
        {
            var byExercise = phases.Where(o => o.training_day <= dayCutoff)
                .GroupBy(o => o.exercise)
                .ToDictionary(o => o.Key, o => o.OrderBy(p => $"{p.training_day.ToString().PadLeft(3, '0')}_{p.time}").ToList());

            foreach (var (exercise, exPhases) in byExercise)
            {
                var stats = new FeaturesForExercise();

                // Calc pre-requisites
                var allProblems = exPhases.SelectMany(phase => phase.problems);
                var responseTimes = allProblems.SelectMany(o => o.answers.Select(o => (decimal)o.response_time)) // Note: doesn't care if correct or not
                    .Select(o => Math.Min(o, 60000)) // cap at 60 seconds
                    .Order()
                    .ToList();

                var lnResponseTimes = responseTimes.Select(t => (decimal)Math.Log((double)t));
                var median = responseTimes[responseTimes.Count() / 2];
                var sdInitial = lnResponseTimes.GetStandardDeviation();
                var cutoff = median + 2.5M * sdInitial;

                Func<decimal, bool> isNotOutlier = val => (decimal)Math.Log((double)val) <= cutoff;
                Func<decimal, bool> isOutlier = val => (decimal)Math.Log((double)val) > cutoff;

                stats.NumProblemsWithAnswers = allProblems.Count(problem => problem.answers.Any());
                stats.PercentCorrect = 100 * allProblems.Count(problem => problem.answers.Any(answer => answer.correct)) / allProblems.Count();

                // Standard deviation:
                stats.StandardDeviation = responseTimes.Where(isNotOutlier).GetStandardDeviation();

                // Highest level reached (with at least one correct answered on that level)
                stats.HighestLevel = allProblems
                    .Where(HasCorrectAnswer)
                    .Max(problem => problem.level);

                // Number of exercises: The number of exercises it took to reach the highest level defined above
                // TODO: just reach level, or with correct answer?
                stats.NumExercises = 1 + exPhases.FindIndex(phase => phase.problems.Any(p => p.level == stats.HighestLevel));

                // 7) Median time correct: The median response time for correctly answered questions after outliers have been removed
                // for exercise = Tangram, Rotation, NVR SO, Mathtest01 and Numbercomparison01
                stats.MedianTimeCorrect = (int)allProblems
                    .Where(HasCorrectAnswer)
                    .Select(o => (decimal)o.answers.First().response_time)
                    .Where(isNotOutlier)
                    .GetMedian();

                // 8) Median time incorrect: The median response time for correctly answered questions minus the median response time for incorrectly answered questions after outliers have been removed
                // for exercise = WM - Grid, Npals, Rotation and Mathtest01
                stats.MedianTimeIncorrect = 
                    stats.MedianTimeCorrect - (int)allProblems
                    .Where(problem => HasCorrectAnswer(problem) == false)
                    .Select(o => (decimal)o.answers.First().response_time)
                    .Where(isNotOutlier)
                    .GetMedian();
                //TODO: not sure this is what is expected

                //9) Number of high response times: The number of questions with a response time above the outlier cutoff
                //for exercise = Npals, Rotation, Numberline, NVR RP, NVR SO and Numbercomparison01
                stats.NumHighResponseTimes = allProblems.Count(problem => problem.answers.Any(answer => isOutlier(answer.response_time)));
                
                //10) Skew: The skew for response times after outliers have been removed
                //for exercise = Mathtest01, Npals, NVR RP, NVR SO, Rotation and Tangram 

                //11) Median level: The median level of correctly answered questions
                //for exercise = Npals, Numberline and NVR RP
                stats.MedianLevel = allProblems.Where(problem => problem.answers.Any(answer => answer.correct))
                    .Select(problem => problem.level)
                    .Order()
                    .GetMedian();
            }

            // Mean time increase: The difference in response time between the question following an incorrectly answered question and the incorrectly answered question (response time after incorrect minus response time incorrect)

            // 12) Training time 20 min: Dummy coded with a 1 if the training time is 20 min and 0 if the training time is 33 min per day.

            // 13) Age 6 - 7: Dummy coded with a 1 if the age is 6 - 7 and a 0 if the age is 7 - 8(other age groups have been excluded from the data set)

            return new MLFeaturesJulia();

            bool HasCorrectAnswer(Problem problem) => problem.answers.Any(answer => answer.correct);
        }

        public class FeaturesForExercise
        {
            public int PercentCorrect { get; set; }
            public int NumProblemsWithAnswers { get; set; }
            public decimal StandardDeviation { get; set; }
            public decimal HighestLevel { get; set; }
            public int NumExercises { get; set; }

            public int MedianTimeCorrect { get; set; }
            public int MedianTimeIncorrect { get; set; }
            
            public int NumHighResponseTimes { get; set; }

            public decimal MedianLevel { get; set; }
            
        }
    }

    public static class StatisticsExtensions
    {
        public static decimal GetMedian(this IEnumerable<decimal> values)
        {
            if (values is not IOrderedEnumerable<decimal>)
                values = values.Order().ToArray();
            var enumerable = values as decimal[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count == 0)
                return 0;
            return enumerable[count / 2];
        }
        public static decimal GetStandardDeviation(this IEnumerable<decimal> values)
        {
            var enumerable = values as decimal[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count > 1)
            {
                var avg = enumerable.Average();
                var sum = enumerable.Sum(d => (d - avg) * (d - avg));
                return (decimal)Math.Sqrt((double)(sum / count));
            }
            return 0;
        }
    }
}
