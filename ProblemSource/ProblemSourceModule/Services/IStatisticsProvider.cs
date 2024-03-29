﻿using Azure.Data.Tables;
using AzureTableGenerics;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSource.Services
{
    public interface IStatisticsProvider
    {
        Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId);
        Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId);
        Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds);
        Task<List<TrainingSummary>> GetAllTrainingSummaries();
    }

    public class StatisticsProvider : IStatisticsProvider
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory;
        private readonly ITypedTableClientFactory typedTableClientFactory;

        public StatisticsProvider(IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory, ITypedTableClientFactory typedTableClientFactory)
        {
            this.userGeneratedDataRepositoryProviderFactory = userGeneratedDataRepositoryProviderFactory;
            this.typedTableClientFactory = typedTableClientFactory;
        }

        private IUserGeneratedDataRepositoryProvider GetDataProvider(int trainingId) =>
            userGeneratedDataRepositoryProviderFactory.Create(trainingId);

        public async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId) =>
            (await GetDataProvider(trainingId).PhaseStatistics.GetAll()).OrderBy(o => o.training_day).ThenBy(o => o.timestamp).ToList();

        public async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId) =>
            (await GetDataProvider(trainingId).TrainingDays.GetAll()).OrderBy(o => o.TrainingDay).ToList();

        public async Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds)
        {
            var result = new List<TrainingSummary?>();
            foreach (var chunk in IEnumerableExtensions.Chunk(trainingIds, 10))
            {
                var tasks = chunk.Select(o => GetDataProvider(o).TrainingSummaries.GetAll());
                var resolved = await Task.WhenAll(tasks);
                result.AddRange(resolved.SelectMany(o => o));
            }
            return result;
        }

        public async Task<List<TrainingSummary>> GetAllTrainingSummaries()
        {
            var q = typedTableClientFactory.TrainingSummaries.QueryAsync<TableEntity>("");
            var converter = new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none"));
            var result = new List<TrainingSummary>();
            await foreach (var item in q)
            {
                result.Add(converter.ToPoco(item));
            }
            return result;
        }
    }
}
