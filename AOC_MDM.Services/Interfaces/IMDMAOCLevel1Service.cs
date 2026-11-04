using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMAOCLevel1Service
    {
        Task<IEnumerable<MDMAOCLevel1DTO>> GetAOCLevel1sAsync();

        Task<IEnumerable<MDMViewAOCLevel1DTO>> GetViewAOCLevel1sAsync();

        Task<IEnumerable<MDMAOCLevel1DTO>> SearchAllColumnsAsync(string searchText);

        Task<IEnumerable<MDMViewAOCLevel1DTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText);

        Task<IEnumerable<string>> GetDistinctTeamsActiveAsync();

        Task<MDMAOCLevel1DTO> GetFilteredAsync(string level1);

        Task<bool> UpdateDirectlyAsync(string level1, MDMAOCLevel1DTO updatedRecord, string checkerId, string remarks);

        Task<MDMAOCLevel3EditDTO> GetRecsWithAffectedAttributesAsync(string columnValue);

        Task BulkUpdateAsync(IEnumerable<MDMAOCLevel1DTO> entities, string checkerId, string role, string? remarks = null);
    }
}
