using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMViewAOCLevel1Service
    {
        Task<PaginatedResult<MDMViewAOCLevel1DTO>> GetDataPlusGlobalSearchAsync(
        int pageNumber,
        int pageSize,
        string? filterColumn = null,
        string? filterValue = null,
        string? searchText = null);

        Task<PaginatedResult<MDMViewAOCLevel1DTO>> GetViewAOCLevel1Async(
    int pageNumber,
    int pageSize,
    string filterColumnName,
    string filterValue);

    }
}
