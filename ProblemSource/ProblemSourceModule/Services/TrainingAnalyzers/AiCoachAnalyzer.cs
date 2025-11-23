using Common;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
	public class AiCoachAnalyzer : ITrainingAnalyzer
	{
		public async Task<string> CreatePrompt(Training training, IUserGeneratedDataRepositoryProvider provider,
			IEnumerable<(Training Training, IUserGeneratedDataRepositoryProvider Provider)> referenceTrainingProviders)
		{
			// TODO: data from... where?
			var generalPlan = """
				* 5 sessions per week, except holiday weeks
				* Thursday sessions will be in a noisy setting
				""".Trim();

			var audience = $"a parent of the trainee (who is {training.AgeBracket} years old)"; // $"the trainee, {training.AgeBracket} years old";

			var earlierCoachingSessions = new[]
			{
				new { Date = DateTimeOffset.UtcNow.AddDays(-5), Notes = "" }
			}.ToList();
			earlierCoachingSessions.Clear();

			var plannedSessions = new[] { new { Date = DateTime.Today } }.ToList(); // TODO: option for user to provide a plan / schedule so we can evaluate adherence
			plannedSessions.Clear();

			var userIsFreeToChooseExercises = false; // TODO: from some training.Settings.

			// TODO: variability in response times, min level as well as max

			var baseLineIsFirstNMinutes = 5;

			var normProviders = referenceTrainingProviders.Where(o => o.Training.AgeBracket == training.AgeBracket);
			var normProvider = normProviders.FirstOrDefault(o => o.Training.Username.EndsWith(o.Training.AgeBracket)).Provider;
			var stdDevProvider = normProviders.FirstOrDefault(o => o.Training.Username.EndsWith("_stddev")).Provider;

			var allDays = (await provider.TrainingDays.GetAll()).OrderBy(o => o.TrainingDay).ToList();
			{
				// TODO: temp
				var tmt = allDays.TakeLast(5).FirstOrDefault()?.StartTime;
				if (tmt != null)
					earlierCoachingSessions.Add(new { Date = new DateTimeOffset(tmt.Value), Notes = "" });
			}

			if (earlierCoachingSessions.Any())
			{
				var detailedInformationFor = allDays.Where(o => o.StartTime >= earlierCoachingSessions.Last().Date).ToList();
				detailedInformationFor = detailedInformationFor.TakeLast(7).ToList(); // limit to last 7 days (lots of data)
				var trainingDays = detailedInformationFor.Select(o => o.TrainingDay).ToList();
				var phases = (await provider.Phases.GetAll()).Where(o => trainingDays.Contains(o.training_day)).ToList();
				// TODO: slow - fetch for relevant days only
				var phaseDetails = phases.OrderBy(o => o.time).SelectMany(phase =>
				{
					// TODO: average / stddev per phase for correct vs incorrect
					// maybe: extra pauses after incorrect answers? higher likelihood of another incorrect answer after a first one?
					// but for some exercises (e.g. WM), level highly affects response time and accuracy, so need to control for that as well.

					var probsWithAnswers = phase.problems.Where(o => o.answers.Any()).ToList();
					var stats = probsWithAnswers.Select((prob, index) =>
						{
							// for response times, only consider first answer.
							var lastEnd = index == 0 ? null : probsWithAnswers[index - 1].answers.LastOrDefault()?.time;
							return new
							{
								Exercise = phase.exercise,
								Start = phase.time,
								Day = phase.training_day,
								Level = prob.level,
								PreviousLevel = index == 0 ? null : (decimal?)probsWithAnswers[index - 1].level,
								TimeSinceLast = lastEnd == null ? null : (long?)(prob.time - lastEnd.Value),
								ResponseTime = prob.answers.First().response_time,
								FirstCorrect = prob.answers.First().correct,
								NumIncorrect = prob.answers.Count(o => !o.correct),
								PreviousCorrect = index == 0 ? null : (bool?)probsWithAnswers[index - 1].answers.First().correct
							};
						}).ToList();
					//	ResponseTimes = new 
					//	{
					//		Correct = stats.Where(o => o.FirstCorrect == true).Select(o => o.ResponseTime).ToList(),
					//		Incorrect = stats.Where(o => o.FirstCorrect == false).Select(o => o.ResponseTime).ToList(),
					//		AfterCorrect = withLastCorrect.Where(o => o.LastCorrect != false).Select(o => o.Data.ResponseTime).ToList(),
					//		AfterIncorrect = withLastCorrect.Where(o => o.LastCorrect == false).Select(o => o.Data.ResponseTime).ToList(),
					//	},
					//	// Probability of failure after previous failure
					//};
					return stats;
				}).ToList();
				var timeForCorrectByExerciseAndLevel = phaseDetails
					.GroupBy(o => o.Exercise)
					.ToDictionary(
						o => o.Key,
						o => o.GroupBy(p => (int)p.Level)
						.ToDictionary(p => p.Key, p => p.Select(q => new { q.FirstCorrect, q.ResponseTime, q.PreviousLevel, q.PreviousCorrect }).ToList()));
			}

			var latestDay = allDays.Max(o => o.TrainingDay);
			var stats = (await provider.PhaseStatistics.GetAll()).OrderBy(o => o.training_day).ToList();

			var excludeExercises = new[] { "mathtest", "numbercomparison" };

			var normDays = normProvider == null ? [] : (await normProvider.TrainingDays.GetAll()).ToList();
			var normStats = normProvider == null ? [] : (await normProvider.PhaseStatistics.GetAll()).ToList();

			var timePerSession = allDays.Select(o => {
				var n = normDays.SingleOrDefault(p => p.TrainingDay == o.TrainingDay);
				return new
				{
					Date = o.StartTime.ToString("yyyy-MM-dd HH:mm"),
					Weekday = o.StartTime.ToString("dddd"),
					DurationMinutes = Math.Round((o.EndTimeStamp - o.StartTime).TotalMinutes),
					ExpectedMinutes = training.Settings.timeLimits[0],
					ActivePercentage = ActivePercentage(o),
					ActivePercentageAgeNorm = n == null ? "" : ActivePercentage(n),
					//AccuracyComparedToNorm = 1.1M
				};
				string ActivePercentage(TrainingDayAccount tda) => $"{Math.Round(100.0 * tda.ResponseMinutes / (tda.RemainingMinutes + tda.ResponseMinutes))}%";
			}).ToList();

			var phaseStatsByBracket = new Dictionary<string, List<PhaseStatistics>>();
			if (normProvider != null)
				phaseStatsByBracket.Add("norm", normStats);
			//foreach (var (bracket, prov) in referenceTrainingProviders)
			//	phaseStatsByBracket.Add(bracket, (await prov.PhaseStatistics.GetAll()).ToList());
			var baselineByExercise = stats.Select(o => ExerciseStats.getSharedId(o.exercise).ToLower()).Distinct()
				.Select(exerciseId =>
				{
					var usersBaseline = GetBaseline(stats, exerciseId, baseLineIsFirstNMinutes);
					var averages = phaseStatsByBracket.Select(p => new { Who = $"Norm", Value = GetBaseline(p.Value, exerciseId, baseLineIsFirstNMinutes) })
						.Concat([new { Who = "This user", Value = usersBaseline }]);
					return new { Exercise = exerciseId, Values = averages.ToList() };
				});
			var baselineByExerciseTable = new[] { new[] { "Exercise" }.Concat(baselineByExercise.First().Values.Select(o => o.Who)).ToList() }
				.Concat(baselineByExercise.Select(o =>
				{
					return new[] { o.Exercise }.Concat(o.Values.Select(p => ToString(p.Value))).ToList();
				})).ToList();

			var allExercises = stats.Concat(normStats == null ? [] : normStats.Where(o => o.training_day <= latestDay))
				.Select(o => ExerciseStats.getSharedId(o.exercise).ToLower()).Distinct().ToList();
			var maxLevelsWithNorm = Enumerable.Range(1, latestDay).Select(day =>
			{
				var byExercise = allExercises
					.Select(o => new { Exercise = o, User = GetMax(stats, o, day), Norm = normStats == null ? null : GetMax(normStats, o, day) })
					.Where(o => o.User != null || o.Norm != null)
					.ToList();
				return new { Day = day, ByExercise = byExercise.ToDictionary(o => o.Exercise, o => new { o.User, o.Norm }) };
			}).ToList();
			var usedExercises = maxLevelsWithNorm.SelectMany(o => o.ByExercise.Keys).Distinct().ToList();
			var maxLevelWithNormTable = new[] { new[] { "Day" }.Concat(usedExercises.SelectMany(o => new[] { "User", "Norm" }.Select(p => $"{o}:{p}"))).ToList() }
				.Concat(maxLevelsWithNorm.Select(o =>
			{
				var xx = usedExercises.SelectMany(p =>
				{
					var tmp = o.ByExercise.TryGetValue(p, out var v) ? v : null;
					return new[] { ToString(tmp?.User), ToString(tmp?.Norm) };
				});
				return new[] { o.Day.ToString() }.Concat(xx).ToList();
			})).ToList();

			var exerciseDescriptions = new Dictionary<string, string> {
				["wm_grid"] = "Working memory - a 4x4 grid where the user needs to recall visuospatial sequences",
				["wm_numbers"] = "Working memory - the user is presented with a digit sequence, then needs to input that sequence but reversed",
				["wm_crush"] = "Working memory - fruits are highlighted in a sequence, recalling it correctly pops the fruits",
				["wm_3dgrid"] = "Working memory - buttons in a pseudo-3d cube are highlighted, user needs to recall the sequence",
				["wm_circle"] = "Working memory - buttons that are rotating in a circle are highlighted, user needs to recall the sequence",
				["wm_moving"] = "Working memory - buttons that moving around in different directions are highlighted, user needs to recall the sequence",

				["tangram"] = "Nonverbal reasoning - solve tangram puzzles",
				["boolean"] = "Nonverbal reasoning - perform boolean operations on overlapping shapes",
				["rotation"] = "Nonverbal reasoning - figure out which shape can be rotated to match the target shape",
				["nvr_so"] = "Nonverbal reasoning - a child-friendly variant of Ravens progressive matrices; figure out missing items in a sequential order",
				["nvr_rp"] = "Nonverbal reasoning - a child-friendly variant of Ravens progressive matrices; figure out missing items in repeated patterns",

				["numberline"] = "Math - user solves different problems presented on a number line",
				["npals"] = "Math - 10-pals and other variants (5-pals on lower levels, 15-pals later on etc)",
				["addsub"] = "Math - simple addition/subtraction under time pressure",
			};
			exerciseDescriptions = exerciseDescriptions.Where(o => baselineByExercise.Any(p => p.Exercise == o.Key)).ToDictionary(o => o.Key, o => o.Value);

			// TODO: Global evaluation vs last N sessions
			var prompt = $"""
				Today is {DateTime.Today:yyyy-MM-dd}. You are acting as a training coach, giving advice to a user of a cognitive training app on how to optimize their training.
				The user is {training.AgeBracket} years old.

				Given the data below (for training id {training.Id}), try to assess of how the training is progressing.
				Try to address the following points:
				{(plannedSessions.Any() ? $"""
				* Is the user doing their expected sessions? Give praise if meeting or exceeding the goals, otherwise ask what made them miss them.
				""" : """
				* Is the user doing the recommended 5 sessions per week?
				""")}
				* During sessions, is the user slacking off or working diligently?
				* Are exercise levels progressing as expected?
					* Is the user having more problems with some types of exercises (e.g. Working Memory)?
				* Is the performance different for some days of the week, or times of day? If so, note how, and ask the user what might be different about them.

				Here are some tips that can be helpful for the user, depending on your evaluation of the points above:
				* If losing focus, take a short break and do 10 push-ups
				* If performance is worse e.g. early mornings or late nights, maybe try other times of day.
					* Maybe some days they're in a different environment (e.g. a busy school setting) - could they find a different location?

				Notes:
				* ActivePercentage metric: important that it's not too low - this would indicate many or long pauses. Note that maximum possible percentage can vary across exercises and client app versions
				* Exercise progress metrics: important that the user continues to get better. On some exercises (e.g. Working memory), the progress is expected to plateau after some time. Refer to the progression of norm data.
				{(userIsFreeToChooseExercises ? "" : "* The order of exercises is decided by an algorithm, so the user cannot choose themselves.")}

				{(generalPlan?.Any() == true
				? $"""
				Here are the general goals that were set before starting the training:
				{generalPlan}

				"""
				: "")}
				## Time per session
				For each session:
				* how long was it ({"DurationMinutes"}) versus how long it should be ({"ExpectedMinutes"})
				* how much time was spent actively solving problems ({"ActivePercentage"}) - the norm (if available) for the age group is "{"ActivePercentageAgeNorm"}"
				{ToMarkdownTable(timePerSession)}

				{(plannedSessions.Any() ? $"""
					## Planned session dates
					This is a plan for when sessions should have been completed.
					{ToMarkdownTable(plannedSessions.Select(o => o.Date.ToString("yyyy-MM-dd")))}
					""" : "")}
				## Exercise baselines
				This is the max level reached after actively using an exercise at least {baseLineIsFirstNMinutes} minutes, compared with average users of different age spans.
				(This user is in the age span {training.AgeBracket})
				{ListToMarkdownTable(baselineByExerciseTable)}

				## Exercise progress
				Here is the user's progression on exercises, compared with the age norm (if available).
				Note that on a given day, the user might not encounter the same exercises as the norm. Focus on the progression and unexpected dips in performance.
				{ListToMarkdownTable(maxLevelWithNormTable)}

				## Exercise descriptions
				For improving your understanding of the data and your evaluation, here are descriptions of the exercises:
				{ToMarkdownTable(exerciseDescriptions.Select(o => new { Id = o.Key, Description = o.Value }))}
				
				{(earlierCoachingSessions.Any() ? $"""
					## Previous notes
					Here are notes from earlier coaching sessions:
					{ToMarkdownTable(earlierCoachingSessions)}
					""" : ""
				)}

				This text will be read by {audience}, so adjust your language and the complexity of the text accordingly.
				""".Trim();

			return prompt;

			string ListToMarkdownTable(List<List<string>> table) 
				=> string.Join("\n", new[] { "-------", string.Join("\n", table.Select(o => string.Join("\t", o))), "--------" });
			string ToMarkdownTable(IEnumerable<object> objs)
			{
				if (!objs.Any())
					return "";
				var first = objs.Where(o => o != null).FirstOrDefault();
				if (first == null)
					return "";

				if (first is System.Collections.IDictionary idi)
				{
				}
				else if (first is System.Collections.IList ili)
				{
					var item1 = ili[0];
					if (item1 != null)
					{
						if (item1 is System.Collections.IList ili2)
						{
							foreach ( var item2 in ili2)
							{
							}
						}
					}
				}
				else
				{
					var props = first.GetType().GetProperties().Where(o => o.CanRead).ToList();
					var table = new List<List<string>>();
					table.Add(props.Select(o => o.Name).ToList());
					foreach (var obj in objs)
					{
						if (obj == null)
							table.Add(new List<string>());
						table.Add(obj == null ? new() : props.Select(o => ToString(o.GetValue(obj))).ToList());
					}
					return ListToMarkdownTable(table);
				}
				return "";
			}

			string ToString(object? obj)
			{
				if (obj == null) return "";
				if (obj is decimal d) return d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
				if (obj is float f) return f.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
				if (obj is double db) return db.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
				return obj.ToString() ?? "";
			}


			decimal? GetMax(List<PhaseStatistics> stats, string exerciseId, int trainingDay)
			{
				var found = stats.Where(o => ExerciseStats.getSharedId(o.exercise).ToLower() == exerciseId && o.training_day == trainingDay);
				return found.Any() ? found.Max(o => o.level_max) : null;
			}

			decimal GetBaseline(List<PhaseStatistics> stats, string exerciseId, int firstMinutes)
			{
				var ordered = stats.Where(o => ExerciseStats.getSharedId(o.exercise).ToLower() == exerciseId).OrderBy(o => o.timestamp).ToList();
				var totalDuration = 0.0;
				var maxLevel = 0M;
				foreach (var item in ordered)
				{
					var duration = (item.end_timestamp - item.timestamp).TotalMinutes;
					totalDuration += duration;
					maxLevel = Math.Max(maxLevel, item.level_max);
					if (totalDuration >= firstMinutes)
						return maxLevel;
				}
				return maxLevel;
			}
		}

		public interface IGameAnalysis
		{
			decimal GetExpectedResponseFactor(Problem p, Answer a);
		}

		public static Dictionary<string, List<XX>> GetTrialAnalysis(IEnumerable<Phase> phases)
		{
			// per exercise: average response time per level
			var tmp = phases.GroupBy(o => o.exercise)
				.ToDictionary(
				byEx => byEx.Key,
				byEx => {
					var responseTimesFirstCorrect = byEx
						.SelectMany(phase =>
							phase.problems.Select(p => new { Level = p.level, Answer = p.answers.FirstOrDefault() }).Where(o => o.Answer?.correct == true))
						.GroupBy(o => (int)o.Level)
						.ToDictionary(
							o => o.Key,
							o => new {
								Avg = decimal.Round(o.Select(p => (decimal)p.Answer!.response_time).Average(), 2),
								SD = decimal.Round(o.Select(p => (decimal)p.Answer!.response_time).StdDev(), 2),
								Count = o.Count()
							});

					var tmp = byEx.SelectMany(phase => phase.problems
					.Select(p => {
						var first = p.answers.FirstOrDefault();
						if (first == null)
							return null;
						var last = p.answers.Last();
						return
						new XX
						{
							Day = phase.training_day,
							Level = p.level,
							Correct = first.correct,
							ResponseTime = first.response_time,
							Tries = first.tries,
							PhaseTime = p.time,
							Time = first.time,
							LastTime = last.time,
							LastResponseTime = last.response_time,
							AnswerCount = p.answers.Count
						};
					})).Where(o => o != null).ToList(); //))).ToList();
					return tmp.OrderBy(o => o.Day).ThenBy(o => o.Time).ToList();
				});

			var statsByGame = tmp.ToDictionary(
				byEx => byEx.Key,
				byEx => byEx.Value.Skip(30).GroupBy(o => (int)o.Level)
				.ToDictionary(
					o => o.Key,
					o => {
						var xx = o.ToList();
						var poa = xx.Select((x, i) =>
						{
							var lastTrial = i > 0 ? xx[i - 1] : null;
							return new {
								TD = lastTrial == null ? 0 : x.Time - lastTrial.Time,
								Last = lastTrial == null ? 0 : lastTrial.ResponseTime,
								TimeDiff = lastTrial == null ? 0 : x.PhaseTime - lastTrial.PhaseTime - lastTrial.ResponseTime,
								TimeDiffX = lastTrial == null ? 0 : x.Time - lastTrial.Time - lastTrial.ResponseTime,
								RespTime = x.ResponseTime,
								PDiff = lastTrial == null ? 0 : x.PhaseTime - lastTrial.PhaseTime
							};
						}).ToList();
						// avg time on incorrect doesn't make sense for WM since input is aborted after first mistake - we'd need to know # input items
						var correct = o.Where(p => p.Correct).ToList();
						return new
						{
							Avg = correct.Any() == false ? 0 :double.Round(correct.Select(p => p.ResponseTime).Average(), 2),
							Count = correct.Count,
							StdDev = correct.Any() == false ? 0 : decimal.Round(correct.Select(p => (decimal)p.ResponseTime).StdDev(), 2),
						};
					})
			);

			var aaa = tmp.Select(kv =>
			{
				var statsByLevel = statsByGame[kv.Key];

				foreach (var trial in kv.Value)
				{
					var stats = statsByLevel[(int)trial.Level];
					var diff = trial.ResponseTime - stats.Avg;
				}
				return 0;
			});

			return tmp;
		}

		public class XX
		{
			public int Day { get; set; }
			public decimal Level { get; set; }
			public bool Correct { get; set; }
			public int ResponseTime { get; set; }
			public int Tries { get; set; }
			public long Time { get; set; }
			public long PhaseTime { get; set; }
			public int LastResponseTime { get; set; }
			public long LastTime { get; set; }
			public int AnswerCount { get; set; }
			public override string ToString() => $"{Level} {Correct} {ResponseTime} {Tries}";
		}

		public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
		{
			throw new NotImplementedException();
		}
	}
}
