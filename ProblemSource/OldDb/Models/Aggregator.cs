using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Aggregator
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ColumnsDefinition { get; set; }
        public int? ColIdForOtherId { get; set; }
        public int? ColIdForLatestUnderlying { get; set; }
        public int? ColIdForAccountId { get; set; }
    }
}
