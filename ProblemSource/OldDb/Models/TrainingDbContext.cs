using Microsoft.EntityFrameworkCore;

namespace OldDb.Models
{
    public partial class TrainingDbContext : DbContext
    {
        public TrainingDbContext()
        {
        }

        public TrainingDbContext(DbContextOptions<TrainingDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Accounts { get; set; } = null!;
        public virtual DbSet<AccountsGroup> AccountsGroups { get; set; } = null!;
        public virtual DbSet<AccountsTransfer> AccountsTransfers { get; set; } = null!;
        public virtual DbSet<Admin> Admins { get; set; } = null!;
        public virtual DbSet<AggregatedCounter> AggregatedCounters { get; set; } = null!;
        public virtual DbSet<AggregatedDatum> AggregatedData { get; set; } = null!;
        public virtual DbSet<Aggregator> Aggregators { get; set; } = null!;
        public virtual DbSet<Answer> Answers { get; set; } = null!;
        public virtual DbSet<ClientLog> ClientLogs { get; set; } = null!;
        public virtual DbSet<Counter> Counters { get; set; } = null!;
        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<Hash> Hashes { get; set; } = null!;
        public virtual DbSet<Impersonation> Impersonations { get; set; } = null!;
        public virtual DbSet<Job> Jobs { get; set; } = null!;
        public virtual DbSet<JobParameter> JobParameters { get; set; } = null!;
        public virtual DbSet<JobQueue> JobQueues { get; set; } = null!;
        public virtual DbSet<List> Lists { get; set; } = null!;
        public virtual DbSet<Person> People { get; set; } = null!;
        public virtual DbSet<Phase> Phases { get; set; } = null!;
        public virtual DbSet<Problem> Problems { get; set; } = null!;
        public virtual DbSet<Schema> Schemas { get; set; } = null!;
        public virtual DbSet<Server> Servers { get; set; } = null!;
        public virtual DbSet<Set> Sets { get; set; } = null!;
        public virtual DbSet<State> States { get; set; } = null!;
        public virtual DbSet<SyncLog> SyncLogs { get; set; } = null!;
        public virtual DbSet<TrainingPlan> TrainingPlans { get; set; } = null!;
        public virtual DbSet<TrainingPlanDatum> TrainingPlanData { get; set; } = null!;
        public virtual DbSet<TransactionHistory> TransactionHistories { get; set; } = null!;
        public virtual DbSet<UsageLog> UsageLogs { get; set; } = null!;
        public virtual DbSet<UserTest> UserTests { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost;Database=trainingdb;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("accounts");

                entity.HasIndex(e => e.ApiKey, "IX_api_key")
                    .IsUnique();

                entity.HasIndex(e => e.CreatedAt, "IX_created_at_covered");

                entity.HasIndex(e => e.LastLoginAt, "IX_last_login_at");

                entity.HasIndex(e => e.PersonId, "IX_person_id");

                entity.HasIndex(e => e.TrainingPlanId, "IX_training_plan_id");

                entity.HasIndex(e => e.Uuid, "IX_uuid")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountsArchiveNotified)
                    .HasColumnType("datetime")
                    .HasColumnName("accounts_archive_notified");

                entity.Property(e => e.ApiKey)
                    .HasMaxLength(36)
                    .HasColumnName("api_key");

                entity.Property(e => e.CompletedExercises).HasColumnName("completed_exercises");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("(((2014)-(1))-(1))");

                entity.Property(e => e.ExerciseStats).HasColumnName("exercise_stats");

                entity.Property(e => e.Finalized).HasColumnName("finalized");

                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

                entity.Property(e => e.LastExerciseCompletedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("last_exercise_completed_at");

                entity.Property(e => e.LastLoginAt)
                    .HasColumnType("datetime")
                    .HasColumnName("last_login_at");

                entity.Property(e => e.LastManualSignInAt)
                    .HasColumnType("datetime")
                    .HasColumnName("last_manual_sign_in_at");

                entity.Property(e => e.LoginCount).HasColumnName("login_count");

                entity.Property(e => e.ManuallyUnlockedExercises)
                    .HasMaxLength(255)
                    .HasColumnName("manually_unlocked_exercises");

                entity.Property(e => e.PersonId).HasColumnName("person_id");

                entity.Property(e => e.PhasesCount).HasColumnName("phases_count");

                entity.Property(e => e.SecondaryTrainingPlanId).HasColumnName("secondary_training_plan_id");

                entity.Property(e => e.TimeLimits)
                    .HasMaxLength(255)
                    .HasColumnName("time_limits");

                entity.Property(e => e.TrainingPlanId).HasColumnName("training_plan_id");

                entity.Property(e => e.TrainingSettings).HasColumnName("training_settings");

                entity.Property(e => e.UserData).HasColumnName("user_data");

                entity.Property(e => e.UtcOffset).HasColumnName("utc_offset");

                entity.Property(e => e.Uuid)
                    .HasMaxLength(50)
                    .HasColumnName("uuid");

                entity.HasOne(d => d.Person)
                    .WithMany(p => p.Accounts)
                    .HasForeignKey(d => d.PersonId)
                    .HasConstraintName("FK_dbo.accounts_dbo.people_person_id");

                entity.HasOne(d => d.SecondaryTrainingPlan)
                    .WithMany(p => p.AccountSecondaryTrainingPlans)
                    .HasForeignKey(d => d.SecondaryTrainingPlanId)
                    .HasConstraintName("FK_dbo.accounts_dbo.secondary_training_plan_id");

                entity.HasOne(d => d.TrainingPlan)
                    .WithMany(p => p.AccountTrainingPlans)
                    .HasForeignKey(d => d.TrainingPlanId)
                    .HasConstraintName("FK_dbo.accounts_dbo.training_plans_training_plan_id");
            });

            modelBuilder.Entity<AccountsGroup>(entity =>
            {
                entity.ToTable("accounts_groups");

                entity.HasIndex(e => e.AccountId, "IX_account_id");

                entity.HasIndex(e => new { e.AccountId, e.GroupId }, "IX_account_id_group_id")
                    .IsUnique();

                entity.HasIndex(e => e.GroupId, "IX_group_id");

                entity.HasIndex(e => e.UpdatedAt, "IX_updated_at");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.GroupId).HasColumnName("group_id");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("(getutcdate())");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.AccountsGroups)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_dbo.accounts_groups_dbo.accounts_account_id");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.AccountsGroups)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("FK_dbo.accounts_groups_dbo.groups_group_id");
            });

            modelBuilder.Entity<AccountsTransfer>(entity =>
            {
                entity.ToTable("accounts_transfer");

                entity.HasIndex(e => e.AccountId, "IX_AccountId_Unique")
                    .IsUnique();

                entity.HasIndex(e => e.ToAdminId, "IX_ToAdmin");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.FromAdminId).HasColumnName("from_admin_id");

                entity.Property(e => e.ToAdminId).HasColumnName("to_admin_id");

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.AccountsTransfer)
                    .HasForeignKey<AccountsTransfer>(d => d.AccountId)
                    .HasConstraintName("FK_accounts_transfer_accounts");

                entity.HasOne(d => d.FromAdmin)
                    .WithMany(p => p.AccountsTransferFromAdmins)
                    .HasForeignKey(d => d.FromAdminId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_accounts_transfer_admins_from");

                entity.HasOne(d => d.ToAdmin)
                    .WithMany(p => p.AccountsTransferToAdmins)
                    .HasForeignKey(d => d.ToAdminId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_accounts_transfer_admins_to");
            });

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("admins");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.CurrentSignInAt)
                    .HasColumnType("datetime")
                    .HasColumnName("current_sign_in_at");

                entity.Property(e => e.CurrentSignInIp)
                    .HasMaxLength(50)
                    .HasColumnName("current_sign_in_ip");

                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .HasColumnName("email");

                entity.Property(e => e.EncryptedPassword)
                    .HasMaxLength(255)
                    .HasColumnName("encrypted_password");

                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

                entity.Property(e => e.LastSignInAt)
                    .HasColumnType("datetime")
                    .HasColumnName("last_sign_in_at");

                entity.Property(e => e.LastSignInIp)
                    .HasMaxLength(50)
                    .HasColumnName("last_sign_in_ip");

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .HasColumnName("name");

                entity.Property(e => e.PermissionLevel).HasColumnName("permission_level");

                entity.Property(e => e.PermissionsGroups)
                    .HasMaxLength(250)
                    .HasColumnName("permissions_groups");

                entity.Property(e => e.RememberCreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("remember_created_at");

                entity.Property(e => e.SessionToken)
                    .HasMaxLength(255)
                    .HasColumnName("session_token");

                entity.Property(e => e.Settings)
                    .IsUnicode(false)
                    .HasColumnName("settings");

                entity.Property(e => e.SignInCount).HasColumnName("sign_in_count");
            });

            modelBuilder.Entity<AggregatedCounter>(entity =>
            {
                entity.HasKey(e => e.Key)
                    .HasName("PK_HangFire_CounterAggregated");

                entity.ToTable("AggregatedCounter", "HangFire");

                entity.HasIndex(e => e.ExpireAt, "IX_HangFire_AggregatedCounter_ExpireAt")
                    .HasFilter("([ExpireAt] IS NOT NULL)");

                entity.Property(e => e.Key).HasMaxLength(100);

                entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<AggregatedDatum>(entity =>
            {
                entity.ToTable("aggregated_data");

                entity.HasIndex(e => new { e.AggregatorId, e.AccountId }, "IX_agg_id_account_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.AggregatorId).HasColumnName("aggregator_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Data).HasColumnName("data");

                entity.Property(e => e.LatestUnderlying)
                    .HasColumnType("datetime")
                    .HasColumnName("latest_underlying");

                entity.Property(e => e.OtherId).HasColumnName("other_id");
            });

            modelBuilder.Entity<Aggregator>(entity =>
            {
                entity.ToTable("aggregators");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ColIdForAccountId).HasColumnName("colIdForAccountId");

                entity.Property(e => e.ColIdForLatestUnderlying).HasColumnName("colIdForLatestUnderlying");

                entity.Property(e => e.ColIdForOtherId).HasColumnName("colIdForOtherId");

                entity.Property(e => e.ColumnsDefinition).HasColumnName("columnsDefinition");

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Answer>(entity =>
            {
                entity.ToTable("answers");

                entity.HasIndex(e => e.ProblemId, "IX_problem_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Answer1)
                    .HasMaxLength(255)
                    .HasColumnName("answer");

                entity.Property(e => e.Correct).HasColumnName("correct");

                entity.Property(e => e.Group)
                    .HasMaxLength(255)
                    .HasColumnName("group");

                entity.Property(e => e.ProblemId).HasColumnName("problem_id");

                entity.Property(e => e.ResponseTime).HasColumnName("response_time");

                entity.Property(e => e.Score)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("score");

                entity.Property(e => e.Time).HasColumnName("time");

                entity.Property(e => e.Tries).HasColumnName("tries");

                entity.HasOne(d => d.Problem)
                    .WithMany(p => p.Answers)
                    .HasForeignKey(d => d.ProblemId)
                    .HasConstraintName("FK_dbo.answers_dbo.problems_problem_id");
            });

            modelBuilder.Entity<ClientLog>(entity =>
            {
                entity.ToTable("client_log");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.Contents)
                    .IsUnicode(false)
                    .HasColumnName("contents");

                entity.Property(e => e.LocalTime)
                    .HasColumnType("datetime")
                    .HasColumnName("local_time");

                entity.Property(e => e.LogSource).HasColumnName("log_source");

                entity.Property(e => e.LogType)
                    .HasMaxLength(50)
                    .HasColumnName("log_type");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.ClientLogs)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_client_log_accounts");
            });

            modelBuilder.Entity<Counter>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Counter", "HangFire");

                entity.HasIndex(e => e.Key, "CX_HangFire_Counter")
                    .IsClustered();

                entity.Property(e => e.ExpireAt).HasColumnType("datetime");

                entity.Property(e => e.Key).HasMaxLength(100);
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("groups");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Hash>(entity =>
            {
                entity.HasKey(e => new { e.Key, e.Field })
                    .HasName("PK_HangFire_Hash");

                entity.ToTable("Hash", "HangFire");

                entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Hash_ExpireAt")
                    .HasFilter("([ExpireAt] IS NOT NULL)");

                entity.Property(e => e.Key).HasMaxLength(100);

                entity.Property(e => e.Field).HasMaxLength(100);
            });

            modelBuilder.Entity<Impersonation>(entity =>
            {
                entity.ToTable("impersonation");

                entity.HasIndex(e => e.ImpersonateeId, "IX_Impersonatee_Unique")
                    .IsUnique();

                entity.HasIndex(e => e.ImpersonatorId, "IX_Impersonator");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.ImpersonateeId).HasColumnName("impersonatee_id");

                entity.Property(e => e.ImpersonatorId).HasColumnName("impersonator_id");

                entity.HasOne(d => d.Impersonatee)
                    .WithOne(p => p.ImpersonationImpersonatee)
                    .HasForeignKey<Impersonation>(d => d.ImpersonateeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_impersonatee_admins");

                entity.HasOne(d => d.Impersonator)
                    .WithMany(p => p.ImpersonationImpersonators)
                    .HasForeignKey(d => d.ImpersonatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_impersonator_admins");
            });

            modelBuilder.Entity<Job>(entity =>
            {
                entity.ToTable("Job", "HangFire");

                entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Job_ExpireAt")
                    .HasFilter("([ExpireAt] IS NOT NULL)");

                entity.HasIndex(e => e.StateName, "IX_HangFire_Job_StateName")
                    .HasFilter("([StateName] IS NOT NULL)");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ExpireAt).HasColumnType("datetime");

                entity.Property(e => e.StateName).HasMaxLength(20);
            });

            modelBuilder.Entity<JobParameter>(entity =>
            {
                entity.HasKey(e => new { e.JobId, e.Name })
                    .HasName("PK_HangFire_JobParameter");

                entity.ToTable("JobParameter", "HangFire");

                entity.Property(e => e.Name).HasMaxLength(40);

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.JobParameters)
                    .HasForeignKey(d => d.JobId)
                    .HasConstraintName("FK_HangFire_JobParameter_Job");
            });

            modelBuilder.Entity<JobQueue>(entity =>
            {
                entity.HasKey(e => new { e.Queue, e.Id })
                    .HasName("PK_HangFire_JobQueue");

                entity.ToTable("JobQueue", "HangFire");

                entity.Property(e => e.Queue).HasMaxLength(50);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.FetchedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<List>(entity =>
            {
                entity.HasKey(e => new { e.Key, e.Id })
                    .HasName("PK_HangFire_List");

                entity.ToTable("List", "HangFire");

                entity.HasIndex(e => e.ExpireAt, "IX_HangFire_List_ExpireAt")
                    .HasFilter("([ExpireAt] IS NOT NULL)");

                entity.Property(e => e.Key).HasMaxLength(100);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Person>(entity =>
            {
                entity.ToTable("people");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.DateOfBirth)
                    .HasColumnType("datetime")
                    .HasColumnName("date_of_birth");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Phase>(entity =>
            {
                entity.ToTable("phases");

                entity.HasIndex(e => e.AccountId, "IX_account_id");

                entity.HasIndex(e => new { e.AccountId, e.Time }, "IX_account_id_time_covered");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.Exercise)
                    .HasMaxLength(255)
                    .HasColumnName("exercise");

                entity.Property(e => e.PhaseType)
                    .HasMaxLength(255)
                    .HasColumnName("phase_type");

                entity.Property(e => e.Sequence).HasColumnName("sequence");

                entity.Property(e => e.Time).HasColumnName("time");

                entity.Property(e => e.TrainingDay).HasColumnName("training_day");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("(((2014)-(1))-(1))");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Phases)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_dbo.phases_dbo.accounts_account_id");
            });

            modelBuilder.Entity<Problem>(entity =>
            {
                entity.ToTable("problems");

                entity.HasIndex(e => e.PhaseId, "IX_phase_id");

                entity.HasIndex(e => e.PhaseId, "IX_phase_id_covered");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Level)
                    .HasColumnType("decimal(5, 1)")
                    .HasColumnName("level");

                entity.Property(e => e.PhaseId).HasColumnName("phase_id");

                entity.Property(e => e.ProblemString)
                    .HasMaxLength(255)
                    .HasColumnName("problem_string");

                entity.Property(e => e.ProblemType)
                    .HasMaxLength(255)
                    .HasColumnName("problem_type");

                entity.Property(e => e.Time).HasColumnName("time");

                entity.HasOne(d => d.Phase)
                    .WithMany(p => p.Problems)
                    .HasForeignKey(d => d.PhaseId)
                    .HasConstraintName("FK_dbo.problems_dbo.phases_phase_id");
            });

            modelBuilder.Entity<Schema>(entity =>
            {
                entity.HasKey(e => e.Version)
                    .HasName("PK_HangFire_Schema");

                entity.ToTable("Schema", "HangFire");

                entity.Property(e => e.Version).ValueGeneratedNever();
            });

            modelBuilder.Entity<Server>(entity =>
            {
                entity.ToTable("Server", "HangFire");

                entity.HasIndex(e => e.LastHeartbeat, "IX_HangFire_Server_LastHeartbeat");

                entity.Property(e => e.Id).HasMaxLength(200);

                entity.Property(e => e.LastHeartbeat).HasColumnType("datetime");
            });

            modelBuilder.Entity<Set>(entity =>
            {
                entity.HasKey(e => new { e.Key, e.Value })
                    .HasName("PK_HangFire_Set");

                entity.ToTable("Set", "HangFire");

                entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Set_ExpireAt")
                    .HasFilter("([ExpireAt] IS NOT NULL)");

                entity.HasIndex(e => new { e.Key, e.Score }, "IX_HangFire_Set_Score");

                entity.Property(e => e.Key).HasMaxLength(100);

                entity.Property(e => e.Value).HasMaxLength(256);

                entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<State>(entity =>
            {
                entity.HasKey(e => new { e.JobId, e.Id })
                    .HasName("PK_HangFire_State");

                entity.ToTable("State", "HangFire");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(20);

                entity.Property(e => e.Reason).HasMaxLength(100);

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.States)
                    .HasForeignKey(d => d.JobId)
                    .HasConstraintName("FK_HangFire_State_Job");
            });

            modelBuilder.Entity<SyncLog>(entity =>
            {
                entity.ToTable("sync_log");

                entity.HasIndex(e => e.AccountId, "IX_account_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.ClientTime)
                    .HasColumnType("datetime")
                    .HasColumnName("client_time");

                entity.Property(e => e.Contents)
                    .IsUnicode(false)
                    .HasColumnName("contents");

                entity.Property(e => e.ServerTime)
                    .HasColumnType("datetime")
                    .HasColumnName("server_time");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.SyncLogs)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_sync_log_accounts");
            });

            modelBuilder.Entity<TrainingPlan>(entity =>
            {
                entity.ToTable("training_plans");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Content).HasColumnName("content");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<TrainingPlanDatum>(entity =>
            {
                entity.ToTable("training_plan_data");

                entity.HasIndex(e => e.TrainingPlanId, "IX_training_plan_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Content).HasColumnName("content");

                entity.Property(e => e.TrainingPlanId).HasColumnName("training_plan_id");

                entity.HasOne(d => d.TrainingPlan)
                    .WithMany(p => p.TrainingPlanData)
                    .HasForeignKey(d => d.TrainingPlanId)
                    .HasConstraintName("FK_dbo.training_plan_data_dbo.training_plans_training_plan_id");
            });

            modelBuilder.Entity<TransactionHistory>(entity =>
            {
                entity.ToTable("__TransactionHistory");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreationTime).HasColumnType("datetime");
            });

            modelBuilder.Entity<UsageLog>(entity =>
            {
                entity.ToTable("usage_log");

                entity.HasIndex(e => e.AccountId, "IX_account_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountId).HasColumnName("account_id");

                entity.Property(e => e.ActionType)
                    .HasMaxLength(50)
                    .HasColumnName("action_type");

                entity.Property(e => e.AdminId).HasColumnName("admin_id");

                entity.Property(e => e.Data)
                    .IsUnicode(false)
                    .HasColumnName("data");

                entity.Property(e => e.Timestamp)
                    .HasColumnType("datetime")
                    .HasColumnName("timestamp");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.UsageLogs)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_usage_log_accounts");

                entity.HasOne(d => d.Admin)
                    .WithMany(p => p.UsageLogs)
                    .HasForeignKey(d => d.AdminId)
                    .HasConstraintName("FK_usage_log_admins");
            });

            modelBuilder.Entity<UserTest>(entity =>
            {
                entity.ToTable("user_tests");

                entity.HasIndex(e => e.PhaseId, "IX_phase_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CompletedPlanet).HasColumnName("completed_planet");

                entity.Property(e => e.Corrects).HasColumnName("corrects");

                entity.Property(e => e.Ended).HasColumnName("ended");

                entity.Property(e => e.Incorrects).HasColumnName("incorrects");

                entity.Property(e => e.PhaseId).HasColumnName("phase_id");

                entity.Property(e => e.PlanetTargetScore).HasColumnName("planet_target_score");

                entity.Property(e => e.Questions).HasColumnName("questions");

                entity.Property(e => e.Score).HasColumnName("score");

                entity.Property(e => e.TargetScore).HasColumnName("target_score");

                entity.Property(e => e.Time).HasColumnName("time");

                entity.Property(e => e.WonRace).HasColumnName("won_race");

                entity.HasOne(d => d.Phase)
                    .WithMany(p => p.UserTests)
                    .HasForeignKey(d => d.PhaseId)
                    .HasConstraintName("FK_dbo.user_tests_dbo.phases_phase_id");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
