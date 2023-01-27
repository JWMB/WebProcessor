using Azure.Data.Tables;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        //private readonly ITypedTableClientFactory tableClientFactory;
        //private readonly int userId;

        public AzureTableUserGeneratedDataRepositoryProvider(ITypedTableClientFactory tableClientFactory, int userId)
        {
            //this.tableClientFactory = tableClientFactory;
            //this.userId = userId;

            var partitionKey = AzureTableConfig.IdToKey(userId);

            phases = new Lazy<IBatchRepository<Phase>>(() =>
                Create(new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases,
                p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey)),
                Phase.UniqueIdWithinUser));
            //Phases = Create(new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases,
            //    p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey)),
            //    Phase.UniqueIdWithinUser);
            trainingDays = new Lazy<IBatchRepository<TrainingDayAccount>>(() =>
                Create(new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays,
                p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), new TableFilter(partitionKey)),
                o => o.TrainingDay.ToString()));
            //TrainingDays = Create(new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays,
            //    p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), new TableFilter(partitionKey)),
            //    o => o.TrainingDay.ToString());
            phaseStatistics = new Lazy<IBatchRepository<PhaseStatistics>>(() =>
                Create(new TableEntityRepository<PhaseStatistics, PhaseStatisticsTableEntity>(tableClientFactory.PhaseStatistics,
                p => p.ToBusinessObject(), p => PhaseStatisticsTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey)),
                Models.Aggregates.PhaseStatistics.UniqueIdWithinUser));
            //PhaseStatistics = Create(new TableEntityRepository<PhaseStatistics, PhaseStatisticsTableEntity>(tableClientFactory.PhaseStatistics, 
            //    p => p.ToBusinessObject(), p => PhaseStatisticsTableEntity.FromBusinessObject(p, userId), new TableFilter(partitionKey)),
            //    Models.Aggregates.PhaseStatistics.UniqueIdWithinUser);

            trainingSummaries = new Lazy<IBatchRepository<TrainingSummary>>(() =>
                Create(new AutoConvertTableEntityRepository<TrainingSummary>(tableClientFactory.TrainingSummaries,
                new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
                t => partitionKey));
            //TrainingSummaries = Create(new AutoConvertTableEntityRepository<TrainingSummary>(tableClientFactory.TrainingSummaries,
            //    new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
            //    t => partitionKey);
            userStates = new Lazy<IBatchRepository<UserGeneratedState>>(() => Create(new AutoConvertTableEntityRepository<UserGeneratedState>(tableClientFactory.UserStates,
                new ExpandableTableEntityConverter<UserGeneratedState>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
                t => partitionKey));
            //UserStates = Create(new AutoConvertTableEntityRepository<UserGeneratedState>(tableClientFactory.UserStates,
            //    new ExpandableTableEntityConverter<UserGeneratedState>(t => new TableFilter("none", partitionKey)), new TableFilter("none", partitionKey)),
            //    t => partitionKey);
        }

        protected virtual IBatchRepository<T> Create<T>(IBatchRepository<T> inner, Func<T, string> createKey) => inner;

        public async Task RemoveAll()
        {
            await Phases.RemoveAll();
            await TrainingDays.RemoveAll();
            await PhaseStatistics.RemoveAll();
            await TrainingSummaries.RemoveAll();
            await UserStates.RemoveAll();
        }

        //public IBatchRepository<Phase> Phases { get; }
        //public IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        //public IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
        //public IBatchRepository<TrainingSummary> TrainingSummaries { get; }
        //public IBatchRepository<UserGeneratedState> UserStates { get; }

        private Lazy<IBatchRepository<Phase>> phases;
        public IBatchRepository<Phase> Phases => phases.Value;

        private Lazy<IBatchRepository<TrainingDayAccount>> trainingDays;
        public IBatchRepository<TrainingDayAccount> TrainingDays => trainingDays.Value;

        private Lazy<IBatchRepository<PhaseStatistics>> phaseStatistics;
        public IBatchRepository<PhaseStatistics> PhaseStatistics => phaseStatistics.Value;

        private Lazy<IBatchRepository<TrainingSummary>> trainingSummaries;
        public IBatchRepository<TrainingSummary> TrainingSummaries => trainingSummaries.Value;
        private Lazy<IBatchRepository<UserGeneratedState>> userStates;
        public IBatchRepository<UserGeneratedState> UserStates => userStates.Value;
    }
}
