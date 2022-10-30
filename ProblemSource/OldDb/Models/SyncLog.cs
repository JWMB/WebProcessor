using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class SyncLog
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public DateTime ServerTime { get; set; }
        public DateTime? ClientTime { get; set; }
        public string? Contents { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
