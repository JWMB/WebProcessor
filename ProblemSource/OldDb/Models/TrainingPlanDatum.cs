using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class TrainingPlanDatum
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public int TrainingPlanId { get; set; }

        public virtual TrainingPlan TrainingPlan { get; set; } = null!;
    }
}
