using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;

namespace ProblemSource.Services
{
    public interface IUserGeneratedRepositoryProvider
    {
        IRepository<Phase> Phases { get; }
        IRepository<TrainingDayAccount> TrainingDays { get; }
        IRepository<PhaseStatistics> PhaseStatistics { get; }
    }

    public class UserGeneratedRepositoriesFactory
    {
        private readonly ITableClientFactory tableClientFactory;

        public UserGeneratedRepositoriesFactory(ITableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;
        }
        public IUserGeneratedRepositoryProvider Create(string userId)
        {
            return new UserGeneratedRepositoryProvider(tableClientFactory, userId);
        }
    }

    public class UserGeneratedRepositoryProvider : IUserGeneratedRepositoryProvider
    {
        public UserGeneratedRepositoryProvider(ITableClientFactory tableClientFactory, string userId)
        {
            Phases = new TableEntityRepository<Phase, PhaseTableEntity>(tableClientFactory.Phases, p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), userId);
            TrainingDays = new TableEntityRepository<TrainingDayAccount, TrainingDayTableEntity>(tableClientFactory.TrainingDays, p => p.ToBusinessObject(), p => TrainingDayTableEntity.FromBusinessObject(p), userId);
            PhaseStatistics = Create<PhaseStatistics>(p => $"{p.timestamp}");

            CachingUserAggregatesRepository<Tx> Create<Tx>(Func<Tx, string> idFunc) => new CachingUserAggregatesRepository<Tx>(new InMemoryRepository<Tx>(idFunc), idFunc);
        }

        public IRepository<Phase> Phases { get; }

        public IRepository<TrainingDayAccount> TrainingDays { get; }

        public IRepository<PhaseStatistics> PhaseStatistics { get; }
    }
}
