namespace TrainingApi.Background
{
	public class TimedHostedService : BackgroundService
	{
		private readonly IEnumerable<IBackgroundProcessor> processors;
		private readonly ILogger<TimedHostedService> _logger;

		public TimedHostedService(IEnumerable<IBackgroundProcessor> processors, ILogger<TimedHostedService> logger)
		{
			this.processors = processors;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation($"{GetType().Name} bg service running");

			await DoWork();
			using PeriodicTimer timer = new(TimeSpan.FromSeconds(30));

			while (await timer.WaitForNextTickAsync(stoppingToken))
				await DoWork();
		}
		private async Task DoWork()
		{
			foreach (var item in processors)
			{
				if (await item.ShouldProcess() == false)
					continue;

				try
				{
					var result = await item.Process();
					_logger.LogInformation($"{GetType().Name} result: {result}");
				}
				catch (OperationCanceledException)
				{
					_logger.LogInformation($"{GetType().Name} bg service stopping");
				}
				//_logger.LogInformation($"Timed Hosted Service is working. Count: {DateTime.UtcNow:HH:mm}");
			}
		}
	}

	public interface IBackgroundProcessor
	{
		Task<bool> ShouldProcess();
		Task<BackgroundProcessorResult> Process();
	}
	public record BackgroundProcessorResult(bool Success, TimeSpan? Duration = null, string? Notes = null, string? Error = null);

	public class ProcessorConfigBase
	{
		public TimeOnly? NotBeforeUtc { get; set; }
		public TimeOnly? NotAfterUtc { get; set; }
		public TimeSpan Interval { get; set; } = TimeSpan.FromHours(23);
		public bool Enabled { get; set; } = true;
	}

	public abstract class BackgroundProcessorBase : IBackgroundProcessor
	{
		private DateTime lastRun = DateTime.MinValue; //note: persist in DB?

		protected BackgroundProcessorBase(ProcessorConfigBase config)
		{
			configBase = config;
		}

		public virtual Task<bool> ShouldProcess()
		{
			var result = false;
			if (configBase.Enabled)
			{
				if (DateTime.UtcNow - lastRun >= configBase.Interval)
					result = IsWithinTimeLimits(DateTime.UtcNow, configBase.NotBeforeUtc, configBase.NotAfterUtc);
			}
			return Task.FromResult(result);
		}

		public static bool IsWithinTimeLimits(DateTime utc, TimeOnly? notBeforeUtc, TimeOnly? notAfterUtc)
		{
			var time = new TimeOnly(utc.TimeOfDay.Hours, utc.TimeOfDay.Minutes);

			if (notBeforeUtc.HasValue == false && notAfterUtc.HasValue == false)
				return true;

			if (notAfterUtc.HasValue == false)
				return time >= notBeforeUtc!.Value;

			if (notBeforeUtc.HasValue == false)
				return time <= notAfterUtc.Value;

			if (notBeforeUtc < notAfterUtc)
				return time >= notBeforeUtc.Value && time <= notAfterUtc.Value;

			return time >= notAfterUtc.Value && time <= notBeforeUtc.Value;
		}

		private int numFailures = 0;
		private int maxFailures = 3;
		private readonly ProcessorConfigBase configBase;

		public virtual async Task<BackgroundProcessorResult> Process()
		{
			if (numFailures > maxFailures)
				return new(false, null, string.Empty, "Too many errors");

			try
			{
				var start = DateTime.UtcNow;
				var result = await ProcessInner();
				var duration = DateTime.UtcNow - start;
				if (result.Duration == null)
					result = result with { Duration = duration };

				lastRun = DateTime.UtcNow;
				numFailures = 0;
				return result;
			}
			catch
			{
				if (++numFailures > maxFailures)
				{
					// TODO: log about turning off due to too many errors
				}
				throw;
			}
		}
		protected abstract Task<BackgroundProcessorResult> ProcessInner();
	}
}