using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMTeamService
    {
        Task<IEnumerable<MDMTeamDTO>> GetTeamsAsync();

        Task<IEnumerable<MDMTeamDTO>> SearchAllColumnsAsync(string searchText);

        Task<IEnumerable<MDMTeamDTO>> GetDataWithOrWithoutFilter(string filterColumn, string filterValue, string searchText);

        Task<MDMTeamDTO> GetFilteredAsync(string team);

        Task<IEnumerable<string>> GetDistinctTeamsActiveAsync();
        // Task<bool> AddSpendAreaAsync(MDMTeamDTO spendAreaDTO);

        Task<bool> UpdateDirectlyAsync(string team, MDMTeamDTO updatedRecord, string checkerId, string remarks);

        Task BulkUpdateAsync(IEnumerable<MDMTeamDTO> entities, string checkerId, string role, string? remarks = null);


    }
}
