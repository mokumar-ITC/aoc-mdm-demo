using AOC_MDM.Core.Entities;

namespace AOC_MDM.Services.Interfaces
{
    public interface IUserAccountService
    {
        Task<UserAccount?> ValidateAsync(string login, string password);
        Task<IEnumerable<UserAccount>> GetAllAsync();
        Task<UserAccount?> GetAsync(string login);
        Task CreateAsync(string login, string displayName, int levelNo, string password);
        Task UpdateAsync(string login, string displayName, int levelNo, bool isActive, string? newPassword);
    }

    public interface IHierarchyService
    {
        Task<HierarchyImpact> GetImpactAsync(string level, string name);
        Task RenameAsync(string level, string oldName, string newName, string userName);
        Task DeleteAsync(string level, string name, string userName);
        Task<IEnumerable<HierarchyAuditEntry>> GetHistoryAsync(string? levelLabel, string? search, int limit = 500);
        Task<string?> CheckCanEnableAsync(string level, string name);
        Task<string> DescribeDisableAsync(string level, string name);
    }
}
