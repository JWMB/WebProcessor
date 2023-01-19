using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using Shouldly;

namespace ProblemSourceModule.Tests
{
    public class MLFeaturesJulia_Tests
    {
        [Fact]
        public void MLFeaturesJulia_ColumnCount()
        {
            var phases = new List<Phase>();
            var trainingSettings = new TrainingSettings { timeLimits = new[] { 33M }.ToList() };
            var features = MLFeaturesJulia.FromPhases(trainingSettings, phases);

            var asArray = features.ToArray();
            asArray.Count().ShouldBe(57);
        }

        [Fact]
        public void MLFeaturesJulia_ExercisesAreGrouped()
        {
            var phases = new List<Phase>
            {
                new Phase
                {
                    exercise = "npals",
                    problems = new List<Problem>
                    {
                        new Problem
                        {
                            level = 2,
                            answers = new List<Answer>{ new Answer { correct = true, response_time = 1000 } }
                        },
                    }
                },
                new Phase
                {
                    exercise = "npals#1",
                    problems = new List<Problem>
                    {
                        new Problem
                        {
                            level = 3,
                            answers = new List<Answer>{ new Answer { correct = true, response_time = 1000 } }
                        },
                    }
                },
                new Phase
                {
                    exercise = "npals#2",
                    problems = new List<Problem>
                    {
                        new Problem
                        {
                            level = 4,
                            answers = new List<Answer>{ new Answer { correct = true, response_time = 1000 } }
                        },
                    }
                }
            };
            var trainingSettings = new TrainingSettings { timeLimits = new[] { 33M }.ToList() };
            var features = MLFeaturesJulia.FromPhases(trainingSettings, phases);
            features.ByExercise["npals"].NumExercisesToHighestLevel.ShouldBe(3);
        }

        [Fact]
        public void MLFeaturesJulia_Simple()
        {
            var exercise = "npals";
            var phases = new List<Phase>
            {
                new Phase
                {
                    exercise = exercise,
                    problems = new List<Problem>
                    {
                        new Problem
                        {
                            level = 4,
                            answers = new List<Answer>{ new Answer { correct = true, response_time = 1000 } }
                        },
                        new Problem
                        {
                            level = 6,
                            answers = new List<Answer>{ new Answer { correct = false, response_time = 1111 } }
                        },
                        new Problem
                        {
                            level = 6,
                            answers = new List<Answer>{ new Answer { correct = true, response_time = 2000 } }
                        }
                    }
                }
            };
            var trainingSettings = new TrainingSettings { timeLimits = new[] { 33M }.ToList() };
            var features = MLFeaturesJulia.FromPhases(trainingSettings, phases);

            var expected = new MLFeaturesJulia
            {
                ByExercise = new Dictionary<string, MLFeaturesJulia.FeaturesForExercise>
                {
                    { exercise, new MLFeaturesJulia.FeaturesForExercise
                        {
                            HighestLevel = 6, MedianLevel = 6, MedianTimeCorrect = 2000, MedianTimeIncorrect = 889, PercentCorrect = 66, NumProblemsWithAnswers = 3, NumExercisesToHighestLevel = 1,
                            StandardDeviation = 447.541680243925m
                        }
                    }
                }
            };

            features.ByExercise[exercise].ShouldBeEquivalentTo(expected.ByExercise[exercise]);

            features.ToArray().ShouldBe(expected.ToArray(), ignoreOrder: false);
        }
    }
}
