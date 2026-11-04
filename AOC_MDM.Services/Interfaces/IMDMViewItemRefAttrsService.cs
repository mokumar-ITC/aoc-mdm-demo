using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMViewItemRefAttrsService
    {
        Task<PaginatedResult<MDMViewItemRefAttrsDTO>> GetDataPlusGlobalSearchAsync(
        int pageNumber,
        int pageSize,
        string? filterColumn = null,
        string? filterValue = null,
        string? searchText = null,string ? sortColumn = null, bool sortAscending = false);

        Task<PaginatedResult<MDMViewItemRefAttrsDTO>> GetViewItemRefAttrsAsync(
int pageNumber,
int pageSize,
string filterColumnName,
string filterValue, string? sortColumn= null, bool sortAscending = false);

    }

}

