using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IMDMItemRefAttrsService
    {
       // Task<IEnumerable<MDMItemRefAttriDTO>> GetAsync();
       // Task<IEnumerable<MDMItemRefAttriDTO>> GetAsync(string Id);

        Task<MDMItemRefAttriDTO> GetFilteredAsync(string itemId);

        Task<MDMItemRefAttriDTO> GetFilteredAsync(string itemId, string source);

        Task<IEnumerable<MDMItemRefAttributesAudit>> ItemAuditGetAsync();
        //Task<IEnumerable<MDMItemRefAttributesAudit>> ItemAuditGetAsync(Int64 Id);

        Task<MDMItemRefAttributesAudit> GetItemAuditAsync(Int64 auditId);


       Task<MDMItemRefAttriApprovalDTO> GetApprovalAsync(string ItemId, string source);

        Task<bool> UpdateReqByMakerAsync(string ItemId, MDMItemRefAttriDTO updatedRecord, string makerId, string remarks);

        Task<IEnumerable<object>> GetDistinctColumnValuesAsync(string columnName);

        Task<bool> ApproveOrRejectUpdateAsync(int auditId, bool isApproved, string checkerId, string remarks);

        Task<bool> UpdateDirectlyAsync(string itemId, MDMItemRefAttriDTO updatedRecord, bool isApproved, string checkerId, string role, string? remarks = null);

        Task BulkUpdateAsync(IEnumerable<MDMViewItemRefAttrsDTO> entities, string checkerId, string role, string? remarks = null);

    
    
    }
}
