using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMViewVendorRefAttrsService
    {
        Task<PaginatedResult<MDMViewVendorRefAttrsDTO>> GetDataPlusGlobalSearchAsync(
        int pageNumber,
        int pageSize,
        string? filterColumn = null,
        string? filterValue = null,
        string? searchText = null);

        Task<PaginatedResult<MDMViewVendorRefAttrsDTO>> GetViewVendorRefAttrsAsync(
    int pageNumber,
    int pageSize,
    string filterColumnName,
    string filterValue);

    }
}
