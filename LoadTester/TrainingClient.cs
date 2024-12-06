using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using System.Net.Http.Json;

internal class TrainingClient
{
    private readonly string username;
    private readonly IHttpClientFactory clientFactory;

    public TrainingClient(string username, IHttpClientFactory clientFactory)
    {
        this.username = username;
        this.clientFactory = clientFactory;
    }

    private GameRunStats GetLatestGameStat(string gameId)
    {
        return new GameRunStats { gameId = gameId, trainingDay = day, highestLevel = 3, lastLevel = 3, won = true };
    }
    //private GameRunStats CreateGameRunStats(IEnumerable<LogItem> items)
    //{
    //}

    public async Task PerformOneDaysTraining(TimeSpan initialPause)
    {
        await Task.Delay(initialPause);

        var startTime = currentTime;

        await EnterDay(1);
        var gameIds = new[] { "WM_grid", "WM_numbers", "WM_moving", "WM_3dgrid" };
        foreach (var gameId in gameIds)
        {
            await EnterGame(gameId);
            Console.WriteLine($"{username}: Enter {gameId}");

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

            await Send();
        }

        var elapsed = currentTime - startTime;

        await ExitDay();
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
    }

    private async Task<long> GetAndOffsetTime(TimeSpan? amount = null)
    {
        if (amount != null)
        {
            currentTime.Add(amount.Value);
            await Delay(amount.Value);
        }
        return CurrentTimestamp;
    }
    private async Task Delay(TimeSpan timeSpan)
    {
        if (true)
            await Task.Delay(timeSpan);
    }

    private long CurrentTimestamp => currentTime.ToUnixTimeSeconds();

    public async Task Send()
    {
        var toSend = log.Concat([new UserStatePushLogItem
        { 
            exercise_stats = new ExerciseStats 
            {
            },
            user_data = new
            {
            }
        }]);

        foreach (var item in toSend)
            item.className = item.GetType().Name;
        //item.type

        var payload = new SyncInput
        {
            Uuid = username,
            SessionToken = sessionId,
            RequestState = sessionId == null,
            CurrentTime = CurrentTimestamp,
            Events = [toSend]
        };

        log.Clear();

        Console.WriteLine($"Send {username} {System.Text.Json.JsonSerializer.Serialize(payload).Length}");
        return;
        using var client = clientFactory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Post, "");
        req.Content = JsonContent.Create(new { A = 1 });
        var response = await client.SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
    }
         
}