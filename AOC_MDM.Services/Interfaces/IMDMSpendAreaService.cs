using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMSpendAreaService
    {
        Task<IEnumerable<MDMSpendAreaDTO>> GetSpendAreaAsync();

        Task<IEnumerable<MDMSpendAreaDTO>> SearchAllColumnsAsync(string searchText);

        Task<MDMSpendAreaDTO> GetFilteredAsync(string id);

        Task<IEnumerable<string>> GetSpendAreaActiveAsync();

        Task<bool> AddSpendAreaAsync(MDMSpendAreaDTO spendAreaDTO);

        Task<bool> UpdateDirectlyAsync(string spendArea, MDMSpendAreaDTO updatedRecord, string checkerId, string remarks);

        Task BulkUpdateAsync(IEnumerable<MDMSpendAreaDTO> entities, string checkerId, string role, string? remarks = null);

    }
}
