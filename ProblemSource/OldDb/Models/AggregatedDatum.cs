using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class AggregatedDatum
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AccountId { get; set; }
        public int? OtherId { get; set; }
        public int AggregatorId { get; set; }
        public DateTime? LatestUnderlying { get; set; }
        public string? Data { get; set; }
    }
}
