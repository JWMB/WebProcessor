using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables.TableEntities;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        public AzureTableUserGeneratedDataRepositoryProvider(ITableClientFactory tableClientFactory, string userId)
        {
            Phases = new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases, p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), userId);
            TrainingDays = new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays, p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), userId);
            PhaseStatistics = new TableEntityRepository<PhaseStatistics, PhaseStatisticsTableEntity>(tableClientFactory.PhaseStatistics, p => p.ToBusinessObject(), p => PhaseStatisticsTableEntity.FromBusinessObject(p, userId), userId);
        }

        public IBatchRepository<Phase> Phases { get; }

        public IBatchRepository<TrainingDayAccount> TrainingDays { get; }

        public IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
    }
}
