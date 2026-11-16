using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    public interface IColumnEditService
    {
        Task<IReadOnlyList<TableEditStatus>> GetStatusAsync();

        // Returns how many columns changed. Columns not in the catalog are ignored.
        Task<int> SaveAsync(string tableKey, IEnumerable<string> editableColumns, string userName);

        // Display names, keyed by column key. A blank name goes back to the built-in label.
        // Returns how many names changed, or an error (nothing is saved) when a name is too long or used twice.
        Task<(int Changed, string? Error)> SaveDisplayNamesAsync(string tableKey, IDictionary<string, string?> displayNames, string userName);

        // Label to show for every catalog column of the table: the admin's display name, else the built-in label
        Task<IReadOnlyDictionary<string, string>> GetLabelsAsync(string tableKey);

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
