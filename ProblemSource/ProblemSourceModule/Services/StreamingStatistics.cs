using ProblemSource.Models.Aggregates;
using System.Collections.Concurrent;

namespace ProblemSourceModule.Services
{
	public interface IStreamingStatistics
	{
		Task Add(int trainingId, Phase phase);
		Task<List<Entry>> Get();

		public async Task Add(int trainingId, IEnumerable<Phase> phases)
		{
			foreach (var entry in phases)
				await Add(trainingId, entry);
		}
		public record Entry(int TrainingId, Phase Phase, DateTime Added);
	}

	public class StreamingStatistics : IStreamingStatistics
	{
		private readonly int maxEntries;
		private ConcurrentQueue<IStreamingStatistics.Entry> queue = new();

		public StreamingStatistics(int maxEntries = 10000)
		{
			this.maxEntries = maxEntries;
		}

		public Task Add(int trainingId, Phase phase)
		{
			queue.Enqueue(new IStreamingStatistics.Entry(trainingId, phase, DateTime.Now));
			if (queue.Count > maxEntries)
				queue.TryDequeue(out _);
			return Task.CompletedTask;
		}

		public Task<List<IStreamingStatistics.Entry>> Get()
		{
			var list = queue.ToList();
			return Task.FromResult(list);
		}
	}
}
