using System.Data.Odbc;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;

namespace AOC_MDM.Infrastrucure.Repository
{
    // Rename / delete / impact / history for the Spend Area -> Team -> Level 1 -> Level 2 -> Level 3 hierarchy.
    // Cascades, integrity and history are enforced by the database (db/postgres/004_hierarchy_integrity_and_users.sql);
    // this class only runs the statements and attributes them to the app user via set_config('mdm.user', ...).
    public class HierarchyRepository : IHierarchyRepository
    {
        private record LevelInfo(string Key, string Label, string Table, string Column, string ParentLabel, string ParentTable, string ParentColumn, string ViewColumn, int Depth);

        // ViewColumn: the column that holds this level in vw_aoc_level_* / vendor & item views
        private static readonly LevelInfo[] Levels =
        {
            new("spendarea", "Spend Area", "spend_area",  "spend_area", null,         null,          null,      "spend_area", 0),
            new("team",      "Team",       "team",        "team",       "Spend Area", "spend_area",  "spend_area", "team",    1),
            new("level1",    "Level 1",    "aoc_level_1", "level_1",    "Team",       "team",        "team",    "level_1",    2),
            new("level2",    "Level 2",    "aoc_level_2", "level_2",    "Level 1",    "aoc_level_1", "level_1", "level_2",    3),
            new("level3",    "Level 3",    "aoc_level_3", "level_3",    "Level 2",    "aoc_level_2", "level_2", "level_3",    4),
        };

        private static readonly string[] PendingAuditTables =
        {
            "vendor_reference_attributes_audit", "item_reference_attributes_audit"
        };

        private readonly OdbcConnection _connection;
        private readonly string _catalog;

        public HierarchyRepository(OdbcConnection connection, string catalog)
        {
            _connection = connection;
            _catalog = catalog;
        }

        private static LevelInfo Info(string level) =>
            Levels.FirstOrDefault(l => l.Key.Equals(level, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown hierarchy level '{level}'.");

        private string T(string table) => $"{_catalog}.mdm.{table}";

        private async Task<long> CountAsync(string sql, params object[] args)
        {
            using var cmd = new OdbcCommand(sql, _connection);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a });
            return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        private async Task<string?> ScalarStringAsync(string sql, params object[] args)
        {
            using var cmd = new OdbcCommand(sql, _connection);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a });
            var v = await cmd.ExecuteScalarAsync();
            return v == null || v == DBNull.Value ? null : v.ToString();
        }

        public async Task<HierarchyImpact> GetImpactAsync(string level, string name)
        {
            var li = Info(level);
            var impact = new HierarchyImpact { Level = li.Key, LevelLabel = li.Label, Name = name, ParentLabel = li.ParentLabel };

            using (var cmd = new OdbcCommand(
                $"SELECT enabled{(li.ParentColumn != null ? $", {li.ParentColumn}" : "")} FROM {T(li.Table)} WHERE {li.Column} = ?", _connection))
            {
                cmd.Parameters.Add(new OdbcParameter { Value = name });
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    impact.Exists = true;
                    impact.Enabled = r.IsDBNull(0) ? null : r.GetString(0);
                    if (li.ParentColumn != null && !r.IsDBNull(1)) impact.ParentName = r.GetString(1);
                }
            }
            if (!impact.Exists) return impact;

            if (impact.ParentName != null)
                impact.ParentEnabled = await ScalarStringAsync($"SELECT enabled FROM {T(li.ParentTable)} WHERE {li.ParentColumn} = ?", impact.ParentName);

            // Everything below this node, using the hierarchy views
            if (li.Depth < 1) impact.Teams = await CountAsync($"SELECT COUNT(*) FROM {T("team")} WHERE spend_area = ?", name);
            if (li.Depth < 2) impact.Level1 = await CountAsync($"SELECT COUNT(*) FROM {T("vw_aoc_level_1")} WHERE {li.ViewColumn} = ?", name);
            if (li.Depth < 3) impact.Level2 = await CountAsync($"SELECT COUNT(*) FROM {T("vw_aoc_level_2")} WHERE {li.ViewColumn} = ?", name);
            if (li.Depth < 4) impact.Level3 = await CountAsync($"SELECT COUNT(*) FROM {T("vw_aoc_level_3")} WHERE {li.ViewColumn} = ?", name);

            // Vendor / item views name Level 1 and Level 2 mdm_level_1 / mdm_level_2
            var mappedColumn = li.ViewColumn is "level_1" or "level_2" ? "mdm_" + li.ViewColumn : li.ViewColumn;
            impact.Vendors = await CountAsync($"SELECT COUNT(*) FROM {T("vw_vendor_reference_attributes")} WHERE {mappedColumn} = ?", name);
            impact.Items = await CountAsync($"SELECT COUNT(*) FROM {T("vw_item_reference_attributes")} WHERE {mappedColumn} = ?", name);

            if (li.Key == "level3")
                foreach (var audit in PendingAuditTables)
                    impact.PendingApprovals += await CountAsync(
                        $"SELECT COUNT(*) FROM {T(audit)} WHERE level_3 = ? AND approvalstatus = 'PendingApproval'", name);

            return impact;
        }

        public async Task RenameAsync(string level, string oldName, string newName, string userName)
        {
            var li = Info(level);
            newName = newName?.Trim() ?? "";
            if (newName.Length == 0) throw new InvalidOperationException("The new name cannot be empty.");
            if (newName == oldName) throw new InvalidOperationException("The new name is the same as the current name.");

            using var tx = _connection.BeginTransaction();
            try
            {
                await ExecAsync(tx, "SELECT set_config('mdm.user', ?, true)", userName);

                // Case-insensitive duplicate check (renaming only the letter case of the same node is allowed)
                var clash = await CountAsync(tx, $"SELECT COUNT(*) FROM {T(li.Table)} WHERE lower({li.Column}) = lower(?) AND {li.Column} <> ?", newName, oldName);
                if (clash > 0) throw new InvalidOperationException($"A {li.Label} named \"{newName}\" already exists.");

                // Foreign keys (ON UPDATE CASCADE) rename the value in every child level, vendor and item
                var rows = await ExecAsync(tx, $"UPDATE {T(li.Table)} SET {li.Column} = ?, updatedby = ?, updateddate = now() WHERE {li.Column} = ?",
                                           newName, userName, oldName);
                if (rows == 0) throw new InvalidOperationException($"{li.Label} \"{oldName}\" was not found.");

                // Pending maker requests still carry the old Level 3 name; keep them valid for approval
                if (li.Key == "level3")
                    foreach (var audit in PendingAuditTables)
                        await ExecAsync(tx, $"UPDATE {T(audit)} SET level_3 = ? WHERE level_3 = ? AND approvalstatus = 'PendingApproval'", newName, oldName);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task DeleteAsync(string level, string name, string userName)
        {
            var li = Info(level);
            var impact = await GetImpactAsync(level, name);
            if (!impact.Exists) throw new InvalidOperationException($"{li.Label} \"{name}\" was not found.");
            if (impact.InUse)
                throw new InvalidOperationException(
                    $"{li.Label} \"{name}\" cannot be deleted: it still has {impact.TotalBelow} level(s) below it, " +
                    $"{impact.Vendors} vendor(s), {impact.Items} item(s) and {impact.PendingApprovals} pending approval(s). Move or delete those first.");

            using var tx = _connection.BeginTransaction();
            try
            {
                await ExecAsync(tx, "SELECT set_config('mdm.user', ?, true)", userName);
                await ExecAsync(tx, $"DELETE FROM {T(li.Table)} WHERE {li.Column} = ?", name);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<HierarchyAuditEntry>> GetHistoryAsync(string? levelLabel, string? search, int limit)
        {
            var where = new List<string>();
            var args = new List<object>();
            if (!string.IsNullOrWhiteSpace(levelLabel)) { where.Add("level_name = ?"); args.Add(levelLabel); }
            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add("(record_key ILIKE ? OR changes ILIKE ? OR changed_by ILIKE ?)");
                var like = $"%{search.Trim()}%";
                args.AddRange(new[] { like, like, like });
            }
            var sql = $@"SELECT audit_id, level_name, record_key, action, changes, changed_by, changed_at
                         FROM {T("hierarchy_audit")}
                         {(where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "")}
                         ORDER BY audit_id DESC LIMIT {Math.Clamp(limit, 1, 5000)}";

            var list = new List<HierarchyAuditEntry>();
            using var cmd = new OdbcCommand(sql, _connection);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a });
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new HierarchyAuditEntry
                {
                    AuditId = Convert.ToInt64(r.GetValue(0)),
                    LevelName = r.GetString(1),
                    RecordKey = r.GetString(2),
                    Action = r.GetString(3),
                    Changes = r.IsDBNull(4) ? null : r.GetString(4),
                    ChangedBy = r.IsDBNull(5) ? null : r.GetString(5),
                    ChangedAt = r.GetDateTime(6),
                });
            return list;
        }

        private async Task<int> ExecAsync(OdbcTransaction tx, string sql, params object[] args)
        {
            using var cmd = new OdbcCommand(sql, _connection, tx);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a });
            return await cmd.ExecuteNonQueryAsync();
        }

        private async Task<long> CountAsync(OdbcTransaction tx, string sql, params object[] args)
        {
            using var cmd = new OdbcCommand(sql, _connection, tx);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a });
            return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }
    }
}
