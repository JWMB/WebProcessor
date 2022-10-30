using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Answer
    {
        public int Id { get; set; }
        public string? Answer1 { get; set; }
        public int ResponseTime { get; set; }
        public long Time { get; set; }
        public int Tries { get; set; }
        public int ProblemId { get; set; }
        public bool Correct { get; set; }
        public decimal Score { get; set; }
        public string? Group { get; set; }

        public virtual Problem Problem { get; set; } = null!;
    }
}
