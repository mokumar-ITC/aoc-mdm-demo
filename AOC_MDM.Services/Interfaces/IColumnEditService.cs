using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IColumnEditService
    {
        Task<IReadOnlyList<TableEditStatus>> GetStatusAsync();

        // Returns how many columns changed. Columns not in the catalog are ignored.
        Task<int> SaveAsync(string tableKey, IEnumerable<string> editableColumns, string userName);

        Task<IReadOnlySet<string>> GetLockedColumnsAsync(string tableKey);
        Task<bool> IsLockedAsync(string tableKey, string columnKey);
        Task<bool> IsRenameLockedAsync(string tableKey);

        // Copies every locked column from the stored record onto the record about to be written.
        // The repositories write every column, so a locked column must carry its stored value, not be left empty.
        Task KeepLockedValuesAsync(string tableKey, object recordToWrite, object storedRecord);

        // Text for the checker when an approval could not apply locked columns; empty when nothing is locked.
        Task<string> DescribeLockedForApprovalAsync(string tableKey);
    }
}
