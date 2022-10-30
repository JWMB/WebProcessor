using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class ClientLog
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int LogSource { get; set; }
        public DateTime LocalTime { get; set; }
        public string LogType { get; set; } = null!;
        public string? Contents { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
