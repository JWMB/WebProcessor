using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class TransactionHistory
    {
        public Guid Id { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
