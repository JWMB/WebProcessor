using Azure.Data.Tables;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        public AzureTableUserGeneratedDataRepositoryProvider(ITypedTableClientFactory tableClientFactory, int userId)
        {
            var partitionKey = AzureTableConfig.IdToKey(userId);

            Phases = new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases,
                p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey));
            TrainingDays = new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays,
                p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), new TableFilter(partitionKey));
            PhaseStatistics = new TableEntityRepository<PhaseStatistics, PhaseStatisticsTableEntity>(tableClientFactory.PhaseStatistics, 
                p => p.ToBusinessObject(), p => PhaseStatisticsTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey));

            TrainingSummaries = new AutoConvertTableEntityRepository<TrainingSummary>(tableClientFactory.TrainingSummaries,
                new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey));
            UserStates = new AutoConvertTableEntityRepository<UserGeneratedState>(tableClientFactory.UserStates,
                new ExpandableTableEntityConverter<UserGeneratedState>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey));
        }

        public IBatchRepository<Phase> Phases { get; }
        public IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        public IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
        public IBatchRepository<TrainingSummary> TrainingSummaries { get; }
        //public IRepository<UserGeneratedState, string> UserStates { get; }
        public IBatchRepository<UserGeneratedState> UserStates { get; }
    }
}
