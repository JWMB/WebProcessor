using Azure.Data.Tables;
using AzureTableGenerics;
using Microsoft.AspNetCore.Mvc;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage.AzureTables
{
    public class AzureTableTrainingSummaryRepository : ITrainingSummaryRepository
    {
        private readonly ITypedTableClientFactory typedTableClientFactory;

        public AzureTableTrainingSummaryRepository(ITypedTableClientFactory typedTableClientFactory)
        {
            this.typedTableClientFactory = typedTableClientFactory;
        }
		public Task<List<TrainingSummary>> GetAll() => GetAllOrByIds(null);

		private async Task<List<TrainingSummary>> GetAllOrByIds(IEnumerable<int>? trainingIds)
		{
			var filter = "";
			if (trainingIds != null)
				filter = $"PartitionKey eq 'none' and ({string.Join(" or ", trainingIds.Select(o => $"RowKey eq '{o.ToString().PadLeft(6, '0')}'"))})";
			var q = typedTableClientFactory.TrainingSummaries.QueryAsync<TableEntity>(filter);
			var converter = new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none"));
			var result = new List<TrainingSummary>();
			await foreach (var item in q)
				result.Add(converter.ToPoco(item));
			return result;
		}

		public Task<List<TrainingSummary>> GetByIds(IEnumerable<int> trainingIds)
			=> GetAllOrByIds(trainingIds);
	}
}
