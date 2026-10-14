using Azure.Data.Tables;
using Force.DeepCloner;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace ProblemSource.Services
{
    public class FromAzureTrainingDataCopier : TrainingDataCopier
	{
		private readonly ITypedTableClientFactory tableClientFactory;

		public FromAzureTrainingDataCopier(IUserGeneratedDataRepositoryProviderFactory providerFactory, ITypedTableClientFactory tableClientFactory, IAggregationService aggregationService)
            : base(providerFactory, aggregationService)
		{
			this.tableClientFactory = tableClientFactory;
		}

		protected override async Task Delete(IEnumerable<Phase> phases, int trainingId)
		{
			var tx = phases.Select(o => new TableTransactionAction(TableTransactionActionType.Delete, PhaseTableEntity.FromBusinessObject(o, trainingId)));
			await tableClientFactory.Phases.SubmitTransactionsBatched(tx); // SubmitTransactionAsync(tx);
		}
	}

	public class TrainingDataCopierPlain : TrainingDataCopier
	{
		public TrainingDataCopierPlain(IUserGeneratedDataRepositoryProviderFactory providerFactory, IAggregationService aggregationService) : base(providerFactory, aggregationService)
		{
		}

		protected override Task Delete(IEnumerable<Phase> phases, int trainingId)
		{
			throw new NotImplementedException();
		}
	}

	public abstract class TrainingDataCopier
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory providerFactory;
        private readonly IAggregationService aggregationService;

        public TrainingDataCopier(IUserGeneratedDataRepositoryProviderFactory providerFactory, IAggregationService aggregationService)
        {
            this.providerFactory = providerFactory;
            this.aggregationService = aggregationService;
        }

		protected abstract Task Delete(IEnumerable<Phase> phases, int trainingId);

		public async Task CopyPhases(int srcId, int dstId, Func<Phase, bool> srcFilter, Func<Phase, Phase>? map = null, Func<Phase, bool>? deleteInDst = null, bool updateAggregates = true)
        //public async Task CopyPhases(IUserGeneratedDataRepositoryProvider srcProvider, int dstId, Func<Phase, bool> srcFilter, Func<Phase, Phase>? map = null, Func<Phase, bool>? deleteInDst = null, bool updateAggregates = true)
        {
			//await CopyPhases(, dstId, srcFilter, map, deleteInDst, updateAggregates);

			var srcProvider = providerFactory.Create(srcId);
			var dstProvider = providerFactory.Create(dstId);

			var data = (await srcProvider.Phases.GetAll())
                .Where(srcFilter);

            if (map != null)
                data = data.Select(map);
            
            if (deleteInDst != null)
            {
                var existing = await dstProvider.Phases.GetAll();
                var toDelete = existing.Where(deleteInDst);

                if (toDelete.Any())
                {
                    await Delete(toDelete, dstId);
				}

                if (updateAggregates)
                {
                    await dstProvider.PhaseStatistics.RemoveAll();
                    await dstProvider.TrainingDays.RemoveAll();
                    await dstProvider.TrainingSummaries.RemoveAll();
                }
            }

            await dstProvider.Phases.Upsert(data);

            if (updateAggregates)
            {
                await aggregationService.UpdateAggregatesFromPhases(dstProvider, data, dstId);

                var state = (await dstProvider.UserStates.GetAll()).MaxBy(o => o.exercise_stats.lastTimeStamp);
                var copy = state.DeepClone();
				// What was this for?
				//var trainingSummary = (await dstProvider.TrainingSummaries.GetAll()).Single();
				//copy.exercise_stats.trainingDay = trainingSummary.TrainedDays; // data.Max(o => o.training_day);
				await dstProvider.UserStates.Upsert(new[] { copy });
            }
        }
    }
}
