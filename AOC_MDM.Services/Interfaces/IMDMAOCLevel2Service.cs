using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMAOCLevel2Service
    {
        Task<IEnumerable<MDMAOCLevel2DTO>> GetAOCLevel2sAsync();

        Task<IEnumerable<MDMAOCLevel2DTO>> SearchAllColumnsAsync(string searchText);

        Task<IEnumerable<MDMViewAOCLevel2DTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText);

        Task<IEnumerable<string>> GetDistinctLEVEL1ActiveAsync();

        Task<IEnumerable<string>> GetDistinctLEVEL2ActiveAsync();

        Task<MDMAOCLevel2DTO> GetFilteredAsync(string level2);

        Task<bool> UpdateDirectlyAsync(string level2, MDMAOCLevel2DTO updatedRecord, string checkerId, string remarks);

        Task<MDMAOCLevel3EditDTO> GetRecsWithAffectedAttributesAsync(string columnValue);

        Task BulkUpdateAsync(IEnumerable<MDMAOCLevel2DTO> entities, string checkerId, string role, string? remarks = null);

    }
}
