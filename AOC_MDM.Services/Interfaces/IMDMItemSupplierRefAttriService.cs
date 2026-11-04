using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMItemSupplierRefAttriService
    {

        Task<MDMItemSupplierRefAttriDTO> GetFilteredAsync(string id);

        Task<MDMItemSupplierRefAttriDTO> GetFilteredAsync(string itemId, string vendorId, string source);

        Task<IEnumerable<MDMItemSupplierRefAttributesAudit>> ItemSupplierAuditGetAsync();
        Task<IEnumerable<MDMItemSupplierRefAttributesAudit>> ItemSupplierAuditGetAsync(Int64 Id);

        //  Task<bool> AddAsync(MDMItemSupplierRefAttriDTO ItemSupplierRefAttriDTO);

        //Task<bool> AddAsync(MDMItemSupplierRefAttriDTO mdmItemSupplierRefAttriDTO, string makerId);

        //Task<bool> AddRecordInMainTableAsync(Int64 auditId,string checkerId, string remarks, string status);

        Task<MDMItemSupplierRefAttriApprovalDTO> GetApprovalAsync(string itemSupplierId, string vendorId, string source);

        Task<bool> UpdateReqByMakerAsync(string itemSupplierId, MDMItemSupplierRefAttriDTO updatedRecord, string makerId, string remarks);
        Task<bool> UpdateDirectlyAsync(string itemId, MDMItemSupplierRefAttriDTO updatedRecord, bool isApproved, string checkerId, string role, string? remarks = null);

        Task BulkUpdateAsync(IEnumerable<MDMViewItemSupplierRefAttrsDTO> entities,  string checkerId, string role, string? remarks = null);


        Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string columnName);

        Task<bool> ApproveOrRejectUpdateAsync(int auditId, bool isApproved, string checkerId, string remarks);

    }
}
