using Common;
using ProblemSource.Models;
using ProblemSource.Models.Statistics;

namespace ProblemSource.Services
{
    public class LogEventsToPhases
    {
        public class Result
        {
            public List<Phase> PhasesCreated { get; } = new();
            public List<Phase> PhasesUpdated { get; } = new();
            public List<string> Errors { get; } = new();
            public List<LogItem> Unprocessed { get; } = new();

            public List<Phase> AllPhases => PhasesUpdated.Concat(PhasesCreated).ToList();
        }

        public static Result Create(IEnumerable<LogItem> logItems, IEnumerable<Phase>? preexisting = null)
        {
            var isAlreadySynced = false;
            var skipToNewSession = false;

            Phase? currentPhase = null;
            Problem? currentProblem = null;
            var result = new Result();

            var isCurrentPhasePreexisting = false;
            var isCurrentProblemPreexisting = false;


            foreach (var item in logItems.Where(o => !(o is UserStatePushLogItem)))
            {
                if (skipToNewSession && item.type != "NEW_SESSION")
                {
                    result.Unprocessed.Add(item);
                    continue;
                }
                skipToNewSession = false;

                if (item.type == "ALREADY_SYNCED")
                    isAlreadySynced = true;
                else if (item.type == "NEW_SESSION" || item.type == "NOT_SYNCED")
                    isAlreadySynced = false;

                if (item is NewPhaseLogItem newPhase)
                {
                    isCurrentPhasePreexisting = false;
                    if (isAlreadySynced)
                    {
                        currentPhase = preexisting?.FirstOrDefault(o => o.time == item.time && o.exercise == newPhase.exercise && o.phase_type == newPhase.phase_type && o.sequence == newPhase.sequence);
                        if (currentPhase == null)
                        {
                            result.Errors.Add($"Old phase not found: {newPhase.time} {newPhase.exercise}");
                            skipToNewSession = true;
                        }
                        else
                        {
                            isCurrentPhasePreexisting = true;
                            //result.PhasesUpdated.Add(currentPhase);
                        }
                    }
                    else
                    {
                        currentPhase = Phase.Create(newPhase);
                        result.PhasesCreated.Add(currentPhase);
                    }
                }
                else
                {
                    if (item is PhaseEndLogItem phaseEnd)
                    {
                        if (currentPhase == null)
                        {
                            result.Unprocessed.Add(item);
                            continue;
                        }
                        currentPhase.user_test = UserTest.Create(phaseEnd);
                        currentPhase = null;
                    }
                    else if (item is NewProblemLogItem newProblem)
                    {
                        if (currentPhase == null)
                        {
                            // Assume it's the latest previously reported phase
                            currentPhase = preexisting?.Where(o => o.time < item.time).OrderBy(o => o.time).LastOrDefault();
                            if (currentPhase == null)
                            {
                                result.Errors.Add($"No phase found for problem time={item.time} {newProblem.problem_type} {newProblem.problem_string}");
                                currentPhase = Phase.CreateUnknown(item.time, preexisting?.Max(o => o.training_day) ?? 0);
                                result.PhasesCreated.Add(currentPhase);
                            }
                        }

                        if (isAlreadySynced)
                        {
                            currentProblem = currentPhase.problems.FirstOrDefault(_ => _.time == item.time && _.problem_type == newProblem.problem_type && _.problem_string == newProblem.problem_string);
                            if (currentProblem == null)
                            {
                                result.Errors.Add($"Old {nameof(Problem)} not found: {newProblem.time} {newProblem.problem_string}");
                                result.Unprocessed.Add(item);
                                continue;
                            }
                        }
                        else
                        {
                            currentProblem = Problem.Create(newProblem);
                            currentPhase.problems.Add(currentProblem);
                        }
                    }
                    else
                    {
                        if (item is AnswerLogItem answer)
                        {
                            if (isAlreadySynced)
                                continue;

                            if (currentPhase == null)
                            {
                                result.Unprocessed.Add(item);
                                result.Errors.Add($"No phase for problem : {item.time} {item.className}");
                                continue;
                            }

                            if (currentProblem == null)
                            {
                                currentPhase.problems.FirstOrDefault(_ => _.time == item.time && _.problem_type == currentProblem.problem_type && _.problem_string == currentProblem.problem_string);

                                result.Errors.Add($"No current problem : {item.time} {item.className}");
                                currentProblem = new Problem();
                            }

                            currentProblem.answers.Add(Answer.Create(answer));
                        }
                    }
                }
            }
            return result;
        }

        private static void UpdateCurrentPhaseEnded(Phase currentPhase)
        {
            // TODO: where does this go? Part of PhaseStatistics instead?
            var numWithCorrect = currentPhase.problems.SumOrDefault(_ => _.answers.Exists(p => p.correct) ? 1 : 0, 0);
            var lastProb = currentPhase.problems.Last();
            var lastTime = lastProb.time;
            if (lastProb.answers.Any())
            {
                var lastAnsw = lastProb.answers.Last();
                lastTime = lastAnsw.time; //TODO: or add lastAnsw.response_time?
            }
            // TODO: this can't be right: currentPhase.time = lastTime; Set somewhere in
        }

        //public static SyncedData CreatePhases(Account account, JArray json)
        //{
        //    var result = new SyncedData();
        //    int currentDay = -1;
        //    Phase currentPhase = null;
        //    Problem currentProblem = null;

        //    //If quit in middle of phase/problem: 
        //    //Either client retains last phase in already sent data but marks as already sent (e.g. insert event "OLD_SESSION before that phase event")
        //    //Or 
        //    //var root = JObject.Parse(json);
        //    //var result = new StringBuilder();
        //    var isAlreadySynced = false;
        //    var skipToNewSession = false;
        //    var numCHCPLoadConfigErrors = 0;
        //    //List<string> errors = new List<string>();
        //    foreach (var item in json) //root.Children())
        //    {
        //        try
        //        {
        //            if (item["type"] == null)
        //            {
        //            }
        //            var type = item["type"] == null ? null : item["type"].Value<string>();
        //            //var type = item["type"].Value<string>();
        //            var time = item["time"].Value<long>();

        //            if (type == "ERROR")
        //            {
        //                var cl = new ClientLog { log_source = 0, log_type = type, contents = item.ToString(Formatting.None), local_time = new DateTime(1970, 1, 1).AddMilliseconds(time) };
        //                //TODO: temporary filter until cause has been found (b/c so many chcp_updateLoadFailed errors send from client 1.4.4)
        //                var wasCHCPLoadConfigError = cl.contents.Contains("Failed to load current application config");
        //                if (wasCHCPLoadConfigError)
        //                {
        //                    numCHCPLoadConfigErrors++;
        //                }
        //                if (!wasCHCPLoadConfigError || numCHCPLoadConfigErrors == 1)
        //                    account.logs.Add(cl);
        //                continue;
        //            }

        //            if (skipToNewSession && type != "NEW_SESSION")
        //                continue;

        //            if (type == "ALREADY_SYNCED") //OLD_SESSION
        //            {
        //                isAlreadySynced = true;
        //            }
        //            else if (type == "NEW_SESSION" || type == "NOT_SYNCED")
        //            {
        //                //string base64String = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        //                //var guid = item["session_guid"].Value<string>();
        //                //guid = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(guid));
        //                isAlreadySynced = false;
        //            }
        //            //else if (type == "TEST_START")
        //            //{
        //            //    //TODO: necessary? notify that a test was started but not ended?
        //            //    //We already know that if the phase and/or its problems and/or their answers aren't complete...
        //            //    //training_day time
        //            //}
        //            else if (type == "NEW_PHASE") // || type == "TEST_START")
        //            {
        //                var trainingDay = item["training_day"].Value<int>();
        //                if (trainingDay > currentDay)
        //                {
        //                    currentDay = trainingDay;
        //                }
        //                ////TODO: what to do with TEST_START?
        //                //if (type == "NEW_PHASE")
        //                //{
        //                currentPhase = new Phase
        //                {
        //                    time = time,
        //                    updated_at = DateTime.Now, // new DateTime(1970, 1, 1).AddMilliseconds(time),
        //                    exercise = item["exercise"].Value<string>(), //(item.Value<string>("exercise").Value<string>(),
        //                    training_day = trainingDay,
        //                    phase_type = RequireValue<string>(item, "phase_type"), //item["phase_type"].Value<string>(),
        //                    sequence = item.Value<int?>("sequence"), //.Value<int>(),
        //                    problems = new List<Problem>(),
        //                    user_tests = new List<UserTest>()
        //                };
        //                currentProblem = null;
        //                if (isAlreadySynced)
        //                {
        //                    currentPhase = account.phases.FirstOrDefault(_ => _.time == currentPhase.time && _.exercise == currentPhase.exercise && _.phase_type == currentPhase.phase_type && _.sequence == currentPhase.sequence);
        //                    if (currentPhase == null)
        //                    {
        //                        result.Errors.Add("Old phase not found: " + account.uuid + "/" + currentPhase.time + "/" + currentPhase.exercise);
        //                        skipToNewSession = true;
        //                    }
        //                }
        //                else
        //                {
        //                    result.Phases.Add(currentPhase);
        //                    account.phases.Add(currentPhase);
        //                }
        //                //}
        //            }
        //            else if (type == "NEW_PROBLEM")
        //            {
        //                currentProblem = new Problem
        //                {
        //                    time = time,
        //                    problem_type = item["problem_type"].Value<string>(),
        //                    level = item["level"].Value<decimal>(),
        //                    problem_string = item["problem_string"].Value<string>(),
        //                    answers = new List<Answer>()
        //                };
        //                if (isAlreadySynced)
        //                {
        //                    currentPhase.problems.FirstOrDefault(_ => _.time == currentProblem.time && _.problem_type == currentProblem.problem_type && _.problem_string == currentProblem.problem_string);
        //                    if (currentPhase == null)
        //                    {
        //                        result.Errors.Add("Old problem not found: " + account.uuid + "/" + currentPhase.id + "/" + currentProblem.time + "/" + currentProblem.problem_type);
        //                        skipToNewSession = true;
        //                    }
        //                }
        //                else
        //                {
        //                    if (currentPhase == null)
        //                    {
        //                        currentPhase = CreateDummyPhase(time, 0);
        //                        result.Errors.Add("No currentPhase for problem: " + item.ToString(Formatting.None));
        //                    }
        //                    currentPhase.problems.Add(currentProblem);
        //                    result.Problems.Add(currentProblem);
        //                }
        //            }
        //            else if (type == "ANSWER" || type == "CORRECT_ANSWER" || type == "WRONG_ANSWER" || type == "QUESTION_ANSWER"
        //                || type == "RESULT_CORRECT" || type == "RESULT_WRONG" || type == "RESULT_ANSWER")
        //            {
        //                if (isAlreadySynced)
        //                    continue;

        //                var currentAnswer = new Answer
        //                {
        //                    time = time,
        //                    answer = item["answer"] != null ? item["answer"].ToString() : null, //.GetValue<string>(item["answer"], null), //.Value<string>(), //TODO: "data"?
        //                                                                                        //answer = item["answer"].Value<string>(),
        //                    correct = item["correct"].Value<bool>(), // item["correct"].Value<int>() == 1, //TODO: wasCorrect - or !type.Contains("WRONG")
        //                    response_time = item["response_time"].Value<int>(), //(int)(time - currentAnswer.time)
        //                    tries = GetValue<int>(item["tries"], 0), //TODO: should be required - item["tries"].Value<int>(),

        //                    score = GetValue<decimal>(item["score"], 0),
        //                    group = GetValue<string>(item["group"], null),
        //                };
        //                var oTimings = item["timings"];
        //                if (oTimings != null && !(oTimings is JValue))
        //                {
        //                    var timings = (JObject)item["timings"];
        //                    if (timings != null)
        //                    {
        //                        var t = timings.ToObject<TimesRegisterStimuliResponse>();
        //                        if (t.ResponseTimes != null && t.ResponseTimes.Count(_ => _ != null) > 0)
        //                        {
        //                            if (currentAnswer.group == null)
        //                            {
        //                                currentAnswer.group = string.Join(",", t.ResponseTimes.Select(_ => _ == null ? "" : _.ToString()));
        //                                if (currentAnswer.group.Length > 255)
        //                                    currentAnswer.group = currentAnswer.group.Remove(250) + "...";
        //                            }
        //                        }
        //                    }
        //                }


        //                if (currentProblem == null)
        //                    result.Errors.Add("No currentProblem for answer: " + item.ToString(Formatting.None));
        //                else
        //                {
        //                    currentProblem.answers.Add(currentAnswer);
        //                    result.Answers.Add(currentAnswer);
        //                }
        //            }
        //            //else if (type == "PHASE_END") //TODO: possibly merge PHASE_END and TEST_END on client
        //            //{
        //            //}
        //            //else if (type == "TEST_END")
        //            else if (type == "PHASE_END")
        //            {
        //                if (isAlreadySynced)
        //                    continue;
        //                //TEST GUIDE MODEL
        //                if (item["phase"].Value<string>() == "TEST") //TODO: in future, save UserTest for all that have questions etc, not only TEST
        //                                                             //Also, we could merge UserTest and Phase tables.
        //                {
        //                    var ut = new UserTest
        //                    {
        //                        time = time,
        //                        questions = item["noOfQuestions"].Value<int>(), //noOfQuestions questions
        //                                                                        //corrects = item["noOfCorrectTotal"].Value<int>(),
        //                                                                        //incorrects = item["noOfCorrect"].Value<int>(),
        //                        corrects = item["noOfCorrect"].Value<int>(), //noOfCorrect corrects
        //                        incorrects = item["noOfInCorrect"].Value<int>(), //noOfInCorrect incorrects

        //                        //TODO: should these be nullable/optional?
        //                        score = item.Value<int?>("score") ?? 0, //item["score"].Value<int>(),
        //                        target_score = item.Value<int?>("targetScore") ?? 0, //item["targetScore"].Value<int>(),
        //                        planet_target_score = item.Value<int?>("planetTargetScore") ?? 0, //item["planetTargetScore"].Value<int>(),
        //                        won_race = item.Value<bool?>("wonRace") ?? false, //item["wonRace"].Value<bool>(),
        //                        completed_planet = item.Value<bool?>("completedPlanet") ?? false, //item["completedPlanet"].Value<bool>(),

        //                        ended = true //TODO:??
        //                    };
        //                    if (currentPhase == null)
        //                    {
        //                        currentPhase = CreateDummyPhase(time, 0);
        //                        account.phases.Add(currentPhase);
        //                        result.Phases.Add(currentPhase);
        //                        result.Errors.Add("No phase for PHASE_END:" + item.ToString(Formatting.None));
        //                    }
        //                    currentPhase.user_tests.Add(ut);
        //                    result.UserTests.Add(ut);
        //                }
        //            }
        //            else if (type == "LEAVE_TEST")
        //            {
        //            }
        //            else if (type == "USER_STATE_PUSH")
        //            {
        //                result.OldState = new State();
        //                //TODO: can be null, how to check nicely with json?
        //                if (item["user_data"] != null)
        //                {
        //                    result.OldState.user_data = account.user_data;
        //                    account.user_data = ToStringOrEmpty(item["user_data"].Value<JObject>());
        //                }
        //                if (item["exercise_stats"] != null)
        //                {
        //                    result.OldState.exercise_stats = account.exercise_stats;
        //                    account.exercise_stats = ToStringOrEmpty(item["exercise_stats"].Value<JObject>());
        //                }
        //                if (false) //Should never be accepted from client
        //                {
        //                    if (item["training_settings"] != null)
        //                    {
        //                        result.OldState.training_settings = account.training_settings;
        //                        account.training_settings = ToStringOrEmpty(item["training_settings"].Value<JObject>());
        //                    }
        //                    if (item["time_limits"] != null)
        //                    {
        //                        result.OldState.time_limits = account.time_limits;
        //                        account.time_limits = ToStringOrEmpty(item["time_limits"].Value<JObject>());
        //                    }
        //                }
        //                //TODO: if "finalized" exists (and is true), disable future logins (was test account)
        //                //
        //                //result.Account_exercise_stats = true;
        //                //result.Account_user_data = true;
        //            }
        //            else if (type == "SYNC")
        //            {
        //                //Ignore these, for client internal use 
        //                //TODO: should be filtered out on client
        //            }
        //            else if (type == "END_OF_DAY")
        //            {
        //                //TODO: close current session
        //            }
        //            else
        //            {
        //                result.Warnings.Add("Unhandled event type: " + item.ToString());
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            result.Errors.Add(ex.Message + ": -- Item:" + item.ToString(Formatting.None));
        //            break;
        //        }
        //    }
        //    //try
        //    //{
        //    //    InsertIntoCache(account, result);
        //    //}
        //    //catch (Exception ex)
        //    //{ }

        //    return result;
        //}
    }
}
