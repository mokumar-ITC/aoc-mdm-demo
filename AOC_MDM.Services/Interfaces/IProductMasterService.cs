using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IProductMasterService
    {
        // nodeId: limit to that hierarchy node and everything below it; search: name, code, path or capacity group;
        // approval: a ProductApproval value (PendingApproval, Approved, Rejected, UpdateDirectly, Unchanged)
        Task<ProductMasterBrowse> BrowseAsync(long? nodeId, string? search, string? status, string? approval = null);
        Task<ProductMasterDetail?> GetDetailAsync(long productId);
        Task<IReadOnlyList<ProductMasterField>> GetFieldsAsync();

        // Maker / checker workflow (same rules as the item master):
        //   level 2 (Maker)  -> the change waits for approval
        //   level 3+ (Checker, Admin) -> the change is applied directly
        Task<ProductEditPage?> GetEditAsync(long productId, int userLevel, ProductEditForm? posted = null);
        Task<ProductWorkflowResult> SubmitEditAsync(ProductEditForm form, string userName, int userLevel, string? roleDesc);
        Task<IReadOnlyList<ProductMasterAudit>> GetPendingAsync();
        Task<ProductReview?> GetReviewAsync(long auditId, string userName);
        Task<ProductWorkflowResult> ApproveAsync(long auditId, string checkerId, int userLevel, string? roleDesc, string? remarks);
        Task<ProductWorkflowResult> RejectAsync(long auditId, string checkerId, int userLevel, string? roleDesc, string? remarks);
    }
}
