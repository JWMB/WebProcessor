using Azure.Data.Tables;
using AzureTableGenerics;
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
        public async Task<List<TrainingSummary>> GetAll()
        {
			var q = typedTableClientFactory.TrainingSummaries.QueryAsync<TableEntity>("");
			var converter = new ExpandableTableEntityConverter<TrainingSummary>(t => new TableFilter("none"));
			var result = new List<TrainingSummary>();
			await foreach (var item in q)
				result.Add(converter.ToPoco(item));
			return result;
        }
    }
}
