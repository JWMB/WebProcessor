using Azure.Data.Tables;
using AzureTableGenerics;

namespace ProblemSource.Services.Storage.AzureTables
{

    public class TableEntityRepositoryBatch<T, TTableEntity> : TableEntityRepository<T, TTableEntity>, IBatchRepository<T> 
        where TTableEntity : class, ITableEntity, new()
    {
        public TableEntityRepositoryBatch(TableClient tableClient, Func<TTableEntity, T> toBusinessObject, Func<T, TTableEntity> toTableEntity, TableFilter keyForFilter)
            : base(tableClient, toBusinessObject, toTableEntity, keyForFilter)
        {
        }
    }

    public class AutoConvertTableEntityRepositoryBatch<T> : TableEntityRepositoryBatch<T, TableEntity> where T : class, new()
    {
        public AutoConvertTableEntityRepositoryBatch(TableClient tableClient, ExpandableTableEntityConverter<T> converter, TableFilter keyFilter)
            : base(tableClient, converter.ToPoco, converter.FromPoco, keyFilter)
        {
        }
    }
}
