using AOC_MDM.Core.Entities;

namespace AOC_MDM.Core.Interfaces
{
    public interface IColumnEditSettingRepository
    {
        Task<IEnumerable<ColumnEditSetting>> GetAllAsync();
        Task<IEnumerable<ColumnEditSetting>> GetByTableAsync(string tableKey);
        Task SaveAsync(string tableKey, string columnKey, bool isEditable, string userName);
    }
}
