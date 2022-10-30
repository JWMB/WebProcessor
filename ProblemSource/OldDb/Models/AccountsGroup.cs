using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class AccountsGroup
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int GroupId { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual Group Group { get; set; } = null!;
    }
}
