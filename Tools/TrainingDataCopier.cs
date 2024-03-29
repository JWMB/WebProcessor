﻿using Azure.Data.Tables;
using Force.DeepCloner;
using Microsoft.Data.Analysis;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace Tools
{
    internal class TrainingDataCopier
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory providerFactory;
        private readonly ITypedTableClientFactory tableClientFactory;
        private readonly IAggregationService aggregationService;

        public TrainingDataCopier(IUserGeneratedDataRepositoryProviderFactory providerFactory, ITypedTableClientFactory tableClientFactory, IAggregationService aggregationService)
        {
            this.providerFactory = providerFactory;
            this.tableClientFactory = tableClientFactory;
            this.aggregationService = aggregationService;
        }

        public async Task CopyPhases(int srcId, int dstId, Func<Phase, bool> srcFilter, Func<Phase, Phase>? map = null, Func<Phase, bool>? deleteInDst = null, bool updateAggregates = true)
        {
            await CopyPhases(providerFactory.Create(srcId),dstId, srcFilter, map, deleteInDst, updateAggregates);
        }

        public async Task CopyPhases(IUserGeneratedDataRepositoryProvider srcProvider, int dstId, Func<Phase, bool> srcFilter, Func<Phase, Phase>? map = null, Func<Phase, bool>? deleteInDst = null, bool updateAggregates = true)
        {
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
                    var tx = toDelete.Select(o => new TableTransactionAction(TableTransactionActionType.Delete, PhaseTableEntity.FromBusinessObject(o, dstId)));
                    await tableClientFactory.Phases.SubmitTransactionsBatched(tx); // SubmitTransactionAsync(tx);
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

                var trainingSummary = (await dstProvider.TrainingSummaries.GetAll()).Single();

                var state = (await dstProvider.UserStates.GetAll()).Single();
                var copy = state.DeepClone();
                copy.exercise_stats.trainingDay = trainingSummary.TrainedDays; // data.Max(o => o.training_day);
                await dstProvider.UserStates.Upsert(new[] { copy });
            }
        }
    }
}
