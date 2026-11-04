using AOC_MDM.Services.DTO;
using System.Data;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMAOCLevel3Service
    {
        Task<IEnumerable<MDMAOCLevel3DTO>> GetAOCLevel3sAsync();

        // Task<bool> AddSpendAreaAsync(MDMTeamDTO spendAreaDTO);

        Task<IEnumerable<MDMAOCLevel3DTO>> SearchAllColumnsAsync(string searchText);

        Task<IEnumerable<MDMViewAOCLevel3DTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText);

        Task<MDMAOCLevel3DTO> GetFilteredAsync(string level3);

        Task<IEnumerable<string>> GetDistinctLEVEL3ActiveAsync();
        Task<bool> UpdateDirectlyAsync(string level3, MDMAOCLevel3DTO updatedRecord, string checkerId, string remarks);

        Task<MDMAOCLevel3EditDTO> GetRecsWithAffectedAttributesAsync(string columnValue);

        Task BulkUpdateAsync(IEnumerable<MDMAOCLevel3DTO> entities, string checkerId, string role, string? remarks = null);



    }
}
