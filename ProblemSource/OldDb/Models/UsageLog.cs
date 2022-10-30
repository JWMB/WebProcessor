using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class UsageLog
    {
        public int Id { get; set; }
        public int? AdminId { get; set; }
        public int? AccountId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; } = null!;
        public string? Data { get; set; }

        public virtual Account? Account { get; set; }
        public virtual Admin? Admin { get; set; }
    }
}
