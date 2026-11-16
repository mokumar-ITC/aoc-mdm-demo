using AOC_MDM.Core.Entities;

namespace AOC_MDM.Core.Interfaces
{
    public interface IProductMasterRepository
    {
        Task<IReadOnlyList<ProductHierarchyNode>> GetHierarchyAsync();
        Task<IReadOnlyList<ProductMasterRecord>> GetProductsAsync();
        Task<ProductMasterRecord?> GetProductAsync(long productId);
        Task<IReadOnlyList<ProductMasterField>> GetFieldsAsync();

        // True when another product (not productId) already uses this name, ignoring case
        Task<bool> NameExistsAsync(string productName, long productId);

        // Checker / Admin: write the values straight to the product (UpdateDirectly) and log it in the audit table
        Task UpdateDirectlyAsync(long productId, ProductMasterValues values, string userName, string role, string? remarks);

        // Maker: store a PendingApproval request and flag the product. Returns the new audit id.
        // Throws InvalidOperationException when the product already has an open request.
        Task<long> SubmitForApprovalAsync(long productId, ProductMasterValues values, string makerId, string role, string? remarks);

        Task<IReadOnlyList<ProductMasterAudit>> GetAuditsAsync(string? approvalStatus = null, long? productId = null);
        Task<ProductMasterAudit?> GetAuditAsync(long auditId);

        // Checker decision. Approve writes `values` to the product; reject leaves the product's values unchanged.
        // Both return false when the request is no longer pending (already decided by someone else).
        Task<bool> ApproveAsync(long auditId, ProductMasterValues values, string checkerId, string role, string? remarks);
        Task<bool> RejectAsync(long auditId, string checkerId, string role, string remarks);
    }
}
