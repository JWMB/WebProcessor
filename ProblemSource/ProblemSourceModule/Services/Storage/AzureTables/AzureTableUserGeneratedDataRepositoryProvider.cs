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

            Phases = Create(new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases,
                p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey)),
                Phase.UniqueIdWithinUser);
            TrainingDays = Create(new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays,
                p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), new TableFilter(partitionKey)),
                o => o.TrainingDay.ToString());
            PhaseStatistics = Create(new TableEntityRepository<PhaseStatistics, PhaseStatisticsTableEntity>(tableClientFactory.PhaseStatistics, 
                p => p.ToBusinessObject(), p => PhaseStatisticsTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey)),
                Models.Aggregates.PhaseStatistics.UniqueIdWithinUser);

            TrainingSummaries = Create(new AutoConvertTableEntityRepository<TrainingSummary>(tableClientFactory.TrainingSummaries,
                new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
                t => partitionKey);
            UserStates = Create(new AutoConvertTableEntityRepository<UserGeneratedState>(tableClientFactory.UserStates,
                new ExpandableTableEntityConverter<UserGeneratedState>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
                t => partitionKey);
        }

        private IBatchRepository<T> Create<T>(IBatchRepository<T> inner, Func<T, string> createKey)
        {
            return inner;
            //return new CachingBatchRepositoryFacade<T>(inner, createKey);
        }

        public IBatchRepository<Phase> Phases { get; }
        public IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        public IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
        public IBatchRepository<TrainingSummary> TrainingSummaries { get; }
        //public IRepository<UserGeneratedState, string> UserStates { get; }
        public IBatchRepository<UserGeneratedState> UserStates { get; }
    }
}
