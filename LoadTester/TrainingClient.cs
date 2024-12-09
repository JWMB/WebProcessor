using Microsoft.EntityFrameworkCore.Storage;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;

public class TrainingClient
{
    private readonly string username;
    private readonly ISyncClient syncClient;
    private readonly bool enablePing;
    private float pauseFactor = 0;
    public TrainingClient(string username, ISyncClient syncClient, bool enablePing = false)
    {
        this.username = username;
        this.syncClient = syncClient;
        this.enablePing = enablePing;
    }

    private GameRunStats GetLatestGameStat(string gameId)
    {
        return new GameRunStats { gameId = gameId, trainingDay = day, highestLevel = 3, lastLevel = 3, won = true };
    }

    private bool isRunning = false;
    private bool skipRegularSync = false;
    public async Task StartTraining(int untilDay = 40, int? numDays = null, float pauseFactor = 0, TimeSpan? initialPause = null, bool skipRegularSync = false)
    {
        if (isRunning)
            throw new Exception("Already running");

        isRunning = true;
        this.skipRegularSync = skipRegularSync;
        this.pauseFactor = pauseFactor;
        await StartOfDaySync();

        if (numDays != null)
            untilDay = Math.Min(untilDay, this.day + numDays.Value);

        if (this.day > untilDay)
            return;

        if (initialPause != null)
            await Task.Delay(initialPause.Value);

        while (this.day <= untilDay)
        {
            Console.WriteLine($"{username}: Start day {this.day}/{untilDay}");
            await PerformOneDaysTraining();
        }

        if (skipRegularSync)
            await Sync(false);

        isRunning = false;
    }

    private async Task PerformOneDaysTraining()
    {
        if (!skipRegularSync)
            await StartOfDaySync();

        var startTime = currentTime;

        await EnterDay(this.day);
        var gameIds = new[] { "WM_grid", "WM_numbers", "WM_moving", "WM_3dgrid" };
        foreach (var gameId in gameIds)
        {
            await EnterGame(gameId);
            Console.WriteLine($"{username}: Day {this.day} Enter {gameId}");

            var lastStat = GetLatestGameStat(gameId);
            var level = lastStat.lastLevel;
            //var newStat = new GameRunStats { gameId = gameId, trainingDay = day, started_at = };
            for (int i = 0; i < 3; i++)
            {
                await EnterPhase();

                var numProblems = 6;
                for (int problemNum = 0; problemNum < numProblems; problemNum++)
                {
                    await AddLogWithTimeOffset(new NewProblemLogItem { time = 3000, level = level, problem_string = "abc", problem_type = "smth" });
                    await AddLogWithTimeOffset(new AnswerLogItem { time = 3000, correct = true });
                }

                await ExitPhase();
            }

            await ExitGame();

            if (!skipRegularSync)
                await Sync(false);
        }

        var elapsed = currentTime - startTime;

        await ExitDay();

        if (skipRegularSync)
            this.day++;
        else
            await Sync(false);
    }

    private List<LogItem> log = [];

    private int day;
    private string? sessionId = null;
    public Task EnterDay(int day)
    {
        sessionId = null;
        this.day = day;
        return Task.CompletedTask;
    }

    private string gameId = string.Empty;
    public async Task EnterGame(string gameId)
    {
        this.gameId = gameId;
        await Delay(TimeSpan.FromSeconds(3));
    }
    public async Task EnterPhase()
    {
        await AddLogWithTimeOffset(new NewPhaseLogItem { time = 3000, exercise = gameId, training_day = day });
    }
    public async Task ExitPhase()
    {
        await AddLogWithTimeOffset(new PhaseEndLogItem { time = 3000 });
    }
    public async Task ExitGame()
    {
        await AddLogWithTimeOffset(new LeaveTestLogItem { time = 3000 });
    }
    public async Task ExitDay()
    {
        await AddLogWithTimeOffset(new EndOfDayLogItem { time = 5000 });
    }

    private DateTimeOffset currentTime = DateTimeOffset.UtcNow;
    private async Task AddLogWithTimeOffset(LogItem item)
    {
        item.time = await GetAndOffsetTime(TimeSpan.FromMilliseconds(item.time));
        log.Add(item);

        if (enablePing)
        {
            if (new[] { typeof(NewPhaseLogItem), typeof(NewProblemLogItem), typeof(AnswerLogItem), typeof(PhaseEndLogItem), typeof(LeaveTestLogItem), typeof(EndOfDayLogItem) }
                .Contains(item.GetType()))
            {
                var body = new SyncInput
                {
                    Uuid = username,
                    CurrentTime = CurrentTimestamp,
                    Events = [item]
                };

                await syncClient.Ping(body);
            }
        }
    }

    private async Task<long> GetAndOffsetTime(TimeSpan? amount = null)
    {
        if (amount != null)
        {
            currentTime = currentTime.Add(amount.Value);
            await Delay(amount.Value);
        }
        return CurrentTimestamp;
    }
    private async Task Delay(TimeSpan timeSpan)
    {
        if (pauseFactor > 0)
            await Task.Delay(timeSpan * pauseFactor);
    }

    private long CurrentTimestamp => currentTime.ToUnixTimeMilliseconds();

    public async Task StartOfDaySync()
    {
        var state = await Sync(true);
        if (state == null)
            throw new NullReferenceException();

        this.day = state.exercise_stats.trainingDay + 1;
    }

    public async Task<UserFullState?> Sync(bool requestState)
    {
        var loggedDays = log.OfType<NewPhaseLogItem>().Select(o => o.training_day);
        var maxDay = loggedDays.Any() ? loggedDays.Max() : 0; // this.day;
        var toSend = log.Concat([new UserStatePushLogItem
        { 
            exercise_stats = new ExerciseStats 
            {
                trainingDay = maxDay
            },
            user_data = new
            {
            }
        }]);

        foreach (var item in toSend)
            item.className = item.GetType().Name;

        var payload = new SyncInput
        {
            Uuid = username,
            SessionToken = sessionId,
            RequestState = requestState,
            CurrentTime = CurrentTimestamp,
            Events = toSend.ToArray()
        };

        log.Clear();

        Console.WriteLine($"Send {username} {System.Text.Json.JsonSerializer.Serialize(payload).Length}");
        var result = await syncClient.Sync(payload);

        return result.state == null || requestState == false
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<UserFullState>(result.state);
    }
}