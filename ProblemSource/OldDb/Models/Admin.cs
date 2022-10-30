using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class Admin
    {
        public Admin()
        {
            AccountsTransferFromAdmins = new HashSet<AccountsTransfer>();
            AccountsTransferToAdmins = new HashSet<AccountsTransfer>();
            ImpersonationImpersonators = new HashSet<Impersonation>();
            UsageLogs = new HashSet<UsageLog>();
        }

        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string EncryptedPassword { get; set; } = null!;
        public DateTime? RememberCreatedAt { get; set; }
        public int SignInCount { get; set; }
        public DateTime? CurrentSignInAt { get; set; }
        public DateTime? LastSignInAt { get; set; }
        public string? LastSignInIp { get; set; }
        public string? Name { get; set; }
        public string? CurrentSignInIp { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? PermissionsGroups { get; set; }
        public string? SessionToken { get; set; }
        public int PermissionLevel { get; set; }
        public string? Settings { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Impersonation? ImpersonationImpersonatee { get; set; }
        public virtual ICollection<AccountsTransfer> AccountsTransferFromAdmins { get; set; }
        public virtual ICollection<AccountsTransfer> AccountsTransferToAdmins { get; set; }
        public virtual ICollection<Impersonation> ImpersonationImpersonators { get; set; }
        public virtual ICollection<UsageLog> UsageLogs { get; set; }
    }
}
