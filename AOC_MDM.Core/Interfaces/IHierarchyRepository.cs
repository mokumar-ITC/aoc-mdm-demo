using AOC_MDM.Core.Entities;

namespace AOC_MDM.Core.Interfaces
{
    // Level keys: spendarea, team, level1, level2, level3
    public interface IHierarchyRepository
    {
        Task<HierarchyImpact> GetImpactAsync(string level, string name);
        Task RenameAsync(string level, string oldName, string newName, string userName);
        Task DeleteAsync(string level, string name, string userName);
        Task<IEnumerable<HierarchyAuditEntry>> GetHistoryAsync(string? levelLabel, string? search, int limit);
    }

    public interface IUserAccountRepository
    {
        Task<IEnumerable<UserAccount>> GetAllAsync();
        Task<UserAccount?> GetAsync(string login);
        Task CreateAsync(UserAccount account);
        Task UpdateAsync(UserAccount account);
        Task SetPasswordAsync(string login, string passwordHash);
        Task TouchLastLoginAsync(string login);
    }
}
