using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMVendorRefAttriService
    {

        Task<MDMVendorRefAttriDTO> GetFilteredAsync(string Id);

        Task<MDMVendorRefAttriDTO> GetFilteredAsync(string id, string source);

        Task<IEnumerable<MDMVendorRefAttributesAudit>> VendorAuditGetAsync();
        Task<MDMVendorRefAttributesAudit> VendorAuditGetAsync(Int64 Id);

        Task<bool> AddAsync(MDMVendorRefAttriDTO mdmVendorRefAttriDTO, string makerId);

        Task<bool> AddRecordInMainTableAsync(Int64 auditId, string checkerId, string remarks, string status);

        Task<MDMVendorRefAttriApprovalDTO> GetApprovalAsync(string vendorId,string source);

        Task<bool> UpdateReqByMakerAsync(string vendorId, MDMVendorRefAttriDTO updatedRecord, string makerId, string remarks);

        Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string columnName);

        Task<bool> ApproveOrRejectUpdateAsync(int auditId, bool isApproved, string checkerId, string remarks);

        Task<bool> UpdateDirectlyAsync(string vendorId, MDMVendorRefAttriDTO updatedRecord, bool isApproved, string checkerId, string role, string? remarks = null);

        Task BulkUpdateAsync(IEnumerable<MDMViewVendorRefAttrsDTO> entities, string checkerId, string role, string? remarks = null);

    }
}
