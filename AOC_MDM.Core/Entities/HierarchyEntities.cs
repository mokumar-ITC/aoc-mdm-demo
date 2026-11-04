namespace AOC_MDM.Core.Entities
{
    // One row of mdm.hierarchy_audit (written by database triggers)
    public class HierarchyAuditEntry
    {
        public long AuditId { get; set; }
        public string LevelName { get; set; }
        public string RecordKey { get; set; }
        public string Action { get; set; }
        public string Changes { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    // What a rename / delete / disable of one hierarchy node would affect
    public class HierarchyImpact
    {
        public string Level { get; set; }
        public string LevelLabel { get; set; }
        public string Name { get; set; }
        public bool Exists { get; set; }
        public string Enabled { get; set; }
        public string ParentLabel { get; set; }
        public string ParentName { get; set; }
        public string ParentEnabled { get; set; }
        public long Teams { get; set; }
        public long Level1 { get; set; }
        public long Level2 { get; set; }
        public long Level3 { get; set; }
        public long Vendors { get; set; }
        public long Items { get; set; }
        public long PendingApprovals { get; set; }

        public long TotalBelow => Teams + Level1 + Level2 + Level3;
        public bool InUse => TotalBelow + Vendors + Items + PendingApprovals > 0;
    }

    // Login account (intranet.user_access)
    public class UserAccount
    {
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public int LevelNo { get; set; }
        public string LevelDesc { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
