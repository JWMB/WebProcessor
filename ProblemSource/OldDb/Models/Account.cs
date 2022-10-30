using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Account
    {
        public Account()
        {
            AccountsGroups = new HashSet<AccountsGroup>();
            ClientLogs = new HashSet<ClientLog>();
            Phases = new HashSet<Phase>();
            SyncLogs = new HashSet<SyncLog>();
            UsageLogs = new HashSet<UsageLog>();
        }

        public int Id { get; set; }
        public string Uuid { get; set; } = null!;
        public int CompletedExercises { get; set; }
        public DateTime? LastExerciseCompletedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int UtcOffset { get; set; }
        public string? ApiKey { get; set; }
        public int LoginCount { get; set; }
        public int? TrainingPlanId { get; set; }
        public string? UserData { get; set; }
        public string? ExerciseStats { get; set; }
        public int? PersonId { get; set; }
        public int? PhasesCount { get; set; }
        public bool Finalized { get; set; }
        public int? SecondaryTrainingPlanId { get; set; }
        public string? TimeLimits { get; set; }
        public string? ManuallyUnlockedExercises { get; set; }
        public string? TrainingSettings { get; set; }
        public DateTime? LastManualSignInAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? AccountsArchiveNotified { get; set; }

        public virtual Person? Person { get; set; }
        public virtual TrainingPlan? SecondaryTrainingPlan { get; set; }
        public virtual TrainingPlan? TrainingPlan { get; set; }
        public virtual AccountsTransfer? AccountsTransfer { get; set; }
        public virtual ICollection<AccountsGroup> AccountsGroups { get; set; }
        public virtual ICollection<ClientLog> ClientLogs { get; set; }
        public virtual ICollection<Phase> Phases { get; set; }
        public virtual ICollection<SyncLog> SyncLogs { get; set; }
        public virtual ICollection<UsageLog> UsageLogs { get; set; }
    }
}
