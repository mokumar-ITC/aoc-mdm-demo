using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AOC_MDM.Services.Service
{
    // Registered per request, so a request sees one consistent set of locks and reads them from the database once.
    public class ColumnEditService : IColumnEditService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ColumnEditService> _logger;
        private readonly Dictionary<string, IReadOnlySet<string>> _lockedByTable = new(StringComparer.OrdinalIgnoreCase);

        public ColumnEditService(IUnitOfWork unitOfWork, ILogger<ColumnEditService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<TableEditStatus>> GetStatusAsync()
        {
            var settings = (await _unitOfWork.ColumnEditSettingRepository.GetAllAsync())
                .ToDictionary(s => (s.TableKey, s.ColumnKey));

            return ColumnEditCatalog.Tables.Select(table => new TableEditStatus(table, table.Columns.Select(column =>
            {
                settings.TryGetValue((table.Key, column.Key), out var setting);
                return new ColumnEditStatus(column, setting?.IsEditable ?? true, setting?.UpdatedBy, setting?.UpdatedDate);
            }).ToList())).ToList();
        }

        public async Task<int> SaveAsync(string tableKey, IEnumerable<string> editableColumns, string userName)
        {
            var table = ColumnEditCatalog.Find(tableKey) ?? throw new InvalidOperationException($"Unknown table \"{tableKey}\".");
            var editable = new HashSet<string>(editableColumns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var locked = await GetLockedColumnsAsync(table.Key);

            var changed = 0;
            foreach (var column in table.Columns)
            {
                var shouldBeEditable = editable.Contains(column.Key);
                if (shouldBeEditable == !locked.Contains(column.Key)) continue;

                await _unitOfWork.ColumnEditSettingRepository.SaveAsync(table.Key, column.Key, shouldBeEditable, userName);
                _logger.LogInformation("{User} set {Table}.{Column} editable = {Editable}", userName, table.Key, column.Key, shouldBeEditable);
                changed++;
            }

            _lockedByTable.Remove(table.Key);
            return changed;
        }

        public async Task<IReadOnlySet<string>> GetLockedColumnsAsync(string tableKey)
        {
            if (_lockedByTable.TryGetValue(tableKey, out var cached)) return cached;

            var settings = await _unitOfWork.ColumnEditSettingRepository.GetByTableAsync(tableKey.ToLowerInvariant());
            IReadOnlySet<string> locked = new HashSet<string>(
                settings.Where(s => !s.IsEditable).Select(s => s.ColumnKey), StringComparer.OrdinalIgnoreCase);
            _lockedByTable[tableKey] = locked;
            return locked;
        }

        public async Task<bool> IsLockedAsync(string tableKey, string columnKey) =>
            (await GetLockedColumnsAsync(tableKey)).Contains(columnKey);

        public async Task<bool> IsRenameLockedAsync(string tableKey)
        {
            var renameColumn = ColumnEditCatalog.Find(tableKey)?.RenameColumn;
            return renameColumn != null && await IsLockedAsync(tableKey, renameColumn);
        }

        public async Task KeepLockedValuesAsync(string tableKey, object recordToWrite, object storedRecord)
        {
            foreach (var column in await GetLockedColumnsAsync(tableKey))
            {
                var target = recordToWrite.GetType().GetProperty(column);
                var source = storedRecord.GetType().GetProperty(column);

                // Refuse the write rather than silently let a locked column through.
                if (target == null || source == null)
                    throw new InvalidOperationException($"Locked column {tableKey}.{column} is not on {recordToWrite.GetType().Name} / {storedRecord.GetType().Name}.");

                target.SetValue(recordToWrite, source.GetValue(storedRecord));
            }
        }

        public async Task<string> DescribeLockedForApprovalAsync(string tableKey)
        {
            var table = ColumnEditCatalog.Find(tableKey);
            var locked = await GetLockedColumnsAsync(tableKey);
            if (table == null || locked.Count == 0) return "";

            var labels = table.Columns.Where(c => locked.Contains(c.Key)).Select(c => c.Label);
            return $" Locked columns were not changed: {string.Join(", ", labels)}.";
        }
    }
}
