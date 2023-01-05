using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables.TableEntities;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        public AzureTableUserGeneratedDataRepositoryProvider(ITypedTableClientFactory tableClientFactory, int userId)
        {
            var partitionKey = AzureTableConfig.IdToKey(userId);
            Phases = new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases, p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), partitionKey);
            TrainingDays = new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays, p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), partitionKey);
            PhaseStatistics = new TableEntityRepository<PhaseStatistics, PhaseStatisticsTableEntity>(tableClientFactory.PhaseStatistics, p => p.ToBusinessObject(), p => PhaseStatisticsTableEntity.FromBusinessObject(p, userId), partitionKey);
        }

        public IBatchRepository<Phase> Phases { get; }

        public IBatchRepository<TrainingDayAccount> TrainingDays { get; }

        public IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
    }
}
