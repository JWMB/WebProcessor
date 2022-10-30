using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class TrainingPlan
    {
        public TrainingPlan()
        {
            AccountSecondaryTrainingPlans = new HashSet<Account>();
            AccountTrainingPlans = new HashSet<Account>();
            TrainingPlanData = new HashSet<TrainingPlanDatum>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Content { get; set; }

        public virtual ICollection<Account> AccountSecondaryTrainingPlans { get; set; }
        public virtual ICollection<Account> AccountTrainingPlans { get; set; }
        public virtual ICollection<TrainingPlanDatum> TrainingPlanData { get; set; }
    }
}
