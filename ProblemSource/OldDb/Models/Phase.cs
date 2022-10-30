using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Phase
    {
        public Phase()
        {
            Problems = new HashSet<Problem>();
            UserTests = new HashSet<UserTest>();
        }

        public int Id { get; set; }
        public int? Sequence { get; set; }
        public string? PhaseType { get; set; }
        public string? Exercise { get; set; }
        public int AccountId { get; set; }
        public long Time { get; set; }
        public int TrainingDay { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual ICollection<Problem> Problems { get; set; }
        public virtual ICollection<UserTest> UserTests { get; set; }
    }
}
