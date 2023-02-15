using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models.Aggregates.ML;
using Shouldly;
using System.Text;
using System.Text.RegularExpressions;

namespace ProblemSourceModule.Tests
{
    public class MLFeaturesJulia_Tests
    {
        [Fact]
        public void MLFeaturesJulia_ColumnCount()
        {
            var phases = new List<Phase>();
            var trainingSettings = TrainingSettings.Default;
            var features = MLFeaturesJulia.FromPhases(trainingSettings, phases, age: 6);

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
            var trainingSettings = TrainingSettings.Default;
            var features = MLFeaturesJulia.FromPhases(trainingSettings, phases, age: 6);
            features.ByExercise["npals"].NumProblemsToHighestLevel.ShouldBe(2);
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
            var trainingSettings = TrainingSettings.Default;
            var features = MLFeaturesJulia.FromPhases(trainingSettings, phases, age: 6);

            var expected = new MLFeaturesJulia
            {
                ByExercise = new Dictionary<string, MLFeaturesJulia.FeaturesForExercise>
                {
                    { exercise, new MLFeaturesJulia.FeaturesForExercise {
                        HighestLevelInt = 6, MedianLevel = 6, 
                        MedianTimeCorrect = 2000, MedianTimeIncorrect = 1111, //889,
                        FractionCorrect = 2M / 3,
                        NumProblemsWithAnswers = 3, NumProblemsToHighestLevel = 2,
                        StandardDeviation = 0, //0.285760205721633m,
                        NumProblems = 3, //447.541680243925m
                        Skew = double.NaN,
                        NumHighResponseTimes = 0,
                        }
                    }
                },
                Age6_7 = true
            };

            //var tmp = Newtonsoft.Json.JsonConvert.SerializeObject(new[] { features.ByExercise[exercise], expected.ByExercise[exercise] });
            features.ByExercise[exercise].ShouldBeEquivalentTo(expected.ByExercise[exercise]);

            features.ToArray().ShouldBe(expected.ToArray(), ignoreOrder: false);
        }

        [Fact]
        public void X()
        {
            //account_id,training_day,exercise,correct,response_time,level,problem_time,phase_type
            //165628,1,npals,0.0,41172.0,0.0,1508137547500,TEST
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "JuliaData") + "\\";
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            var csvInput = File.ReadLines($"{path}raw_small.csv");
            var answerRows = Preprocess(csvInput).Skip(1).Select(items =>
            {
                return new
                {
                    account_id = int.Parse(items[0]),
                    training_day = int.Parse(items[1]),
                    exercise = items[2],
                    correct = GetAsInt(3) == 1,
                    response_time = GetAsInt(4),
                    level = GetAsInt(5),
                    problem_time = long.Parse(items[6]),
                    phase_type = items[7],
                    isValid = items[3].Length > 0 && items[4].Length > 0
                };

                int GetAsInt(int col, int fallback = 0)
                {
                    var val = items[col];
                    if (val.Length == 0) return fallback;
                    var index = val.IndexOf(".");
                    return int.Parse(index > 0 ? val.Remove(index) : val);
                }
            })
                //.Where(o => o.training_day <= 5)
                //.Where(o => o.isValid)
                .ToList();

            var result = answerRows.GroupBy(o => o.account_id).Select(byTraining => {
                var allPhases = byTraining
                    .GroupBy(o => o.training_day)
                    .Select(byDay => {
                        var phases = new List<Phase>();
                        var currentPhase = new Phase();
                        var rows = byDay; //.OrderBy(o => o.problem_time).ToList();
                        foreach (var item in rows)
                        {
                            if (item.exercise != currentPhase.exercise || item.isValid == false || item.phase_type != currentPhase.phase_type)
                            {
                                currentPhase = new Phase { exercise = item.exercise, phase_type = item.phase_type, time = item.problem_time, training_day = byDay.Key };
                                phases.Add(currentPhase);
                            }

                            var problem = new Problem { time = item.problem_time, level = item.level };
                            var answer = new Answer { correct = item.correct, time = item.problem_time, response_time = item.response_time };
                            problem.answers.Add(answer);

                            //if (item.isValid)
                            currentPhase.problems.Add(problem);
                        }
                        return phases;
                    }).SelectMany(o => o.ToList());

                return new { Id = byTraining.Key, Phases = allPhases.ToList() };
            }).ToList();

            //,age,training_plan_id,training_time
            //165628,_ages 6 - 7,216,20
            var csvInputPersonal = File.ReadLines($"{path}student_info_small.csv");
            var personal = Preprocess(csvInputPersonal).Skip(1)
                .Select(items => {
                    return new { 
                        Id = int.Parse(items[0]),
                        age = items[1],
                        training_plan_id = int.Parse(items[2]),
                        training_time = int.Parse(items[3])
                    };
                })
                .ToDictionary(o => o.Id, o => o);


            var joined = personal.Join(result, pers => pers.Key, phases => phases.Id, (pers, phases) => new { phases.Id, phases.Phases, pers.Value.training_time, pers.Value.age });
            joined = joined.Where(o => o.Id == 165628); // Note: for testing
            var analyzed = joined
                .ToDictionary(o => o.Id, o => new {
                    Phases = o.Phases,
                    Features = MLFeaturesJulia.FromPhases(
                        new TrainingSettings {timeLimits = new List<decimal> { o.training_time } },
                        o.Phases,
                        age: int.Parse(Regex.Match(o.age, @"\d").Value),
                        dayCutoff: 9999) // Note: Julia didn't filter out day > 5
                });

            //,npals_correct,WM_grid_correct,numberline_correct,mathTest01_correct,nvr_rp_correct,nvr_so_correct,numberComparison01_correct,npals_count,tangram_count,numberline_count,mathTest01_count,nvr_rp_count,nvr_so_count,numberComparison01_count,WM_grid_std,npals_std,numberline_std,rotation_std,nvr_rp_std,mathTest01_std,numberComparison01_std,npals_highest_lev,numberline_highest_lev,nvr_so_highest_lev,nvr_rp_highest_lev,npals_nr_exercises,tangram_nr_exercises,numberline_nr_exercises,rotation_nr_exercises,nvr_rp_nr_exercises,mean_time_increase,tangram_median_time_correct,rotation_median_time_correct,nvr_so_median_time_correct,mathTest01_median_time_correct,numberComparison01_median_time_correct,WM_grid_median_time_incorrect,npals_median_time_incorrect,rotation_median_time_incorrect,mathTest01_median_time_incorrect,npals_nr_high_response_times,rotation_nr_high_response_times,numberline_nr_high_response_times,nvr_rp_nr_high_response_times,nvr_so_nr_high_response_times,numberComparison01_nr_high_response_times,mathTest01_skew,npals_skew,nvr_rp_skew,nvr_so_skew,rotation_skew,tangram_skew,npals_level_median,numberline_level_median,nvr_rp_level_median
            //165628,0.7661691542288557,0.5163934426229508,0.9545454545454546,0.25,,,0.7575757575757576,201.0,3.0,22.0,12.0,0.0,0.0,132.0,0.1346023858324998,0.5052603410268197,0.5863718301635045,1.287361531392319,0.0,0.451205704798035,0.7727476893212469,2.0,2.0,,,61.0,1.0,7.5,inf,0.0,0.8703879192921731,29668.0,1824.0,,4538.0,982.0,464.0,-167.0,-550.0,-2447.0,46.0,13.0,1.0,,,3.0,0.8396975402876315,0.8590440426948066,,,2.107076198011605,1.2411856387021605,0.0,1.0,
            var csvOutput = File.ReadLines($"{path}preprocessed_small.csv");
            var exercisesAndColumns = GetColumnsByExercise(Preprocess(csvOutput.Take(1)).First().Skip(1).ToArray());

            //var tmp = analyzed.First().Value.Features.GetRelevantFeatures();

            var outputRows = Preprocess(csvOutput).Skip(1)
                .Select(items => {
                    return new { Id = int.Parse(items[0]), Features = items.Skip(1).ToList() };
                }).ToDictionary(o => o.Id, o => o.Features);

            var compared = Compare(outputRows.Keys.First());
            Console.WriteLine(compared);

            string Compare(int id)
            {
                var julias = outputRows[id];
                var mine = analyzed[id].Features.ToArray();

                var sb = new StringBuilder();
                foreach (var kv in exercisesAndColumns)
                {
                    sb.AppendLine(kv.Key);
                    foreach (var item in kv.Value)
                    {
                        sb.AppendLine($"\t{item.property}");
                        sb.AppendLine($"\t\t{ItemToString(julias[item.index])}");
                        sb.AppendLine($"\t\t{ItemToString(mine[item.index])}");
                    }
                }

                return sb.ToString();
                //return $"{ArrayToString(julias)}\n{ArrayToString(mine)}";
                //string ArrayToString(IEnumerable<string> items) => string.Join(",", items.Select(ItemToString));
                string ItemToString(string item) => item == "inf" ? item : decimal.Parse(item.Length == 0 ? "0" : item.Replace(",", ".")).ToString("#.##"); //(item.Length > 5 ? item.Remove(5) : item);
            }

            IEnumerable<string[]> Preprocess(IEnumerable<string> lines) => lines.Select(o => o.Trim()).Where(o => o.Length > 0).Select(o => o.Split(','));

            Dictionary<string, List<(string property, int index)>> GetColumnsByExercise(string[] columnsX)
            {
                var columns = columnsX.Select((o, i) => new { Exercise = SplitColumnName(o).exercise, Property = SplitColumnName(o).property, Index = i });
                return columns.GroupBy(o => o.Exercise).ToDictionary(o => o.Key, o => o.Select(p => (p.Property, p.Index)).ToList());

                (string exercise, string property) SplitColumnName(string columnName)
                {
                    if (columnName.Length == 0) return ("N/A", "N/A");
                    columnName = columnName.ToLower();
                    var startIndex = 
                        columnName.StartsWith("nvr_") ? "nvr_".Length + 1 :
                            (columnName.StartsWith("wm_") ? "wm_".Length + 1 : 0);
                    var index = columnName.IndexOf("_", startIndex);
                    return (columnName.Substring(0, index), columnName.Substring(index + 1));
                }
            }
        }
    }
}
