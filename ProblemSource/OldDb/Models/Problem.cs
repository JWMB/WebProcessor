using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Problem
    {
        public Problem()
        {
            Answers = new HashSet<Answer>();
        }

        public int Id { get; set; }
        public long Time { get; set; }
        public string? ProblemType { get; set; }
        public string? ProblemString { get; set; }
        public int PhaseId { get; set; }
        public decimal Level { get; set; }

        public virtual Phase Phase { get; set; } = null!;
        public virtual ICollection<Answer> Answers { get; set; }
    }
}
