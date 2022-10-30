using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Impersonation
    {
        public int Id { get; set; }
        public int ImpersonatorId { get; set; }
        public int ImpersonateeId { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Admin Impersonatee { get; set; } = null!;
        public virtual Admin Impersonator { get; set; } = null!;
    }
}
