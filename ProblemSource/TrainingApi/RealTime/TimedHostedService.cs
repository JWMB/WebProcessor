namespace TrainingApi.RealTime
{
    // https://stackoverflow.com/questions/52163500/net-core-web-api-with-queue-processing
    // https://medium.com/medialesson/run-and-manage-periodic-background-tasks-in-asp-net-core-6-with-c-578a31f4b7a3
    // https://docs.microsoft.com/en-us/aspnet/core/signalr/background-services?view=aspnetcore-6.0
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0&tabs=visual-studio
    public class TimedHostedService : BackgroundService
    {
        private readonly Func<CancellationToken, Task> workFunction;
        private readonly ILogger<TimedHostedService> logger;
        private readonly TimeSpan period = TimeSpan.FromSeconds(5);
        private int executionCount = 0;
        public bool IsEnabled { get; set; } = true;

        public TimedHostedService(Func<CancellationToken, Task> workFunction,
            ILogger<TimedHostedService> logger)
        {
            this.workFunction = workFunction;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(period);
            while (
                !stoppingToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    if (IsEnabled)
                    {
                        await workFunction.Invoke(stoppingToken);
                        executionCount++;
                        logger.LogInformation($"Executed PeriodicHostedService - Count: {executionCount}");
                    }
                    else
                    {
                        logger.LogInformation("Skipped PeriodicHostedService");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation(
                        $"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
                }
            }

            logger.LogInformation($"{GetType().Name} running.");

            //var timer = new Timer(DoWorkX, stoppingToken, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            //await DoWork(stoppingToken);
        }

        //private void DoWorkX(object? state)
        //{
        //    _ = DoWork((CancellationToken)state);
        //}

        //private async Task DoWork(CancellationToken stoppingToken)
        //{
        //    logger.LogInformation(
        //        "Consume Scoped Service Hosted Service is working.");

        //    await workFunction.Invoke(stoppingToken);
        //    //using (var scope = Services.CreateScope())
        //    //{
        //    //    var scopedProcessingService =
        //    //        scope.ServiceProvider
        //    //            .GetRequiredService<IScopedProcessingService>();

        //    //    await scopedProcessingService.DoWork(stoppingToken);
        //    //}
        //}

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }

    public class TimedHostedServiceInner : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly Func<Task> workFunction;
        private readonly ILogger<TimedHostedServiceInner> logger;
        private Timer? timer = null;
        private CancellationToken? cancellationToken;

        public TimedHostedServiceInner(Func<Task> workFunction, ILogger<TimedHostedServiceInner> logger)
        {
            this.workFunction = workFunction;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.cancellationToken != null)
                throw new Exception("Already running - stop first");

            this.cancellationToken = cancellationToken;

            logger.LogInformation("Timed Hosted Service running.");

            timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            //await workFunction.Invoke();
            logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Timed Hosted Service is stopping.");

            timer?.Change(Timeout.Infinite, 0);

            cancellationToken = null;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
