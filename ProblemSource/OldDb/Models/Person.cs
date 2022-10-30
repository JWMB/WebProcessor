using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Person
    {
        public Person()
        {
            Accounts = new HashSet<Account>();
        }

        public int Id { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Name { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }
    }
}
