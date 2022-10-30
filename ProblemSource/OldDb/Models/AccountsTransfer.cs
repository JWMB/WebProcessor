using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class AccountsTransfer
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int FromAdminId { get; set; }
        public int ToAdminId { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual Admin FromAdmin { get; set; } = null!;
        public virtual Admin ToAdmin { get; set; } = null!;
    }
}
