using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Group
    {
        public Group()
        {
            AccountsGroups = new HashSet<AccountsGroup>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<AccountsGroup> AccountsGroups { get; set; }
    }
}
