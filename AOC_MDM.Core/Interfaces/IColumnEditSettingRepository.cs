using AOC_MDM.Core.Entities;

namespace AOC_MDM.Core.Interfaces
{
    public interface IColumnEditSettingRepository
    {
        Task<IEnumerable<ColumnEditSetting>> GetAllAsync();
        Task<IEnumerable<ColumnEditSetting>> GetByTableAsync(string tableKey);
        Task SaveAsync(string tableKey, string columnKey, bool isEditable, string userName);

        // null displayName goes back to the built-in label. Leaves is_editable as it is.
        Task SaveDisplayNameAsync(string tableKey, string columnKey, string? displayName, string userName);
    }
}
