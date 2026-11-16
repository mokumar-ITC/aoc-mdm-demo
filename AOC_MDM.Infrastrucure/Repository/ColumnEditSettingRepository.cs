using System.Data.Odbc;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;

namespace AOC_MDM.Infrastrucure.Repository
{
    public class ColumnEditSettingRepository : IColumnEditSettingRepository
    {
        private readonly OdbcConnection _connection;
        private readonly string _table;

        private const string Columns = "table_key, column_key, is_editable, updated_by, updated_date, display_name";

        public ColumnEditSettingRepository(OdbcConnection connection, string catalog)
        {
            _connection = connection;
            _table = $"{catalog}.mdm.column_edit_setting";
        }

        public Task<IEnumerable<ColumnEditSetting>> GetAllAsync() =>
            QueryAsync($"SELECT {Columns} FROM {_table}");

        public Task<IEnumerable<ColumnEditSetting>> GetByTableAsync(string tableKey) =>
            QueryAsync($"SELECT {Columns} FROM {_table} WHERE table_key = ?", tableKey);

        public async Task SaveAsync(string tableKey, string columnKey, bool isEditable, string userName)
        {
            using var cmd = new OdbcCommand(
                $"INSERT INTO {_table} (table_key, column_key, is_editable, updated_by, updated_date) VALUES (?, ?, ?, ?, now()) " +
                "ON CONFLICT (table_key, column_key) DO UPDATE SET " +
                "is_editable = EXCLUDED.is_editable, updated_by = EXCLUDED.updated_by, updated_date = now()", _connection);
            cmd.Parameters.Add(new OdbcParameter { Value = tableKey });
            cmd.Parameters.Add(new OdbcParameter { Value = columnKey });
            cmd.Parameters.Add(new OdbcParameter { Value = isEditable });
            cmd.Parameters.Add(new OdbcParameter { Value = (object?)userName ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveDisplayNameAsync(string tableKey, string columnKey, string? displayName, string userName)
        {
            using var cmd = new OdbcCommand(
                $"INSERT INTO {_table} (table_key, column_key, display_name, updated_by, updated_date) VALUES (?, ?, ?, ?, now()) " +
                "ON CONFLICT (table_key, column_key) DO UPDATE SET " +
                "display_name = EXCLUDED.display_name, updated_by = EXCLUDED.updated_by, updated_date = now()", _connection);
            cmd.Parameters.Add(new OdbcParameter { Value = tableKey });
            cmd.Parameters.Add(new OdbcParameter { Value = columnKey });
            cmd.Parameters.Add(new OdbcParameter { Value = (object?)displayName ?? DBNull.Value });
            cmd.Parameters.Add(new OdbcParameter { Value = (object?)userName ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<IEnumerable<ColumnEditSetting>> QueryAsync(string sql, params object[] args)
        {
            var list = new List<ColumnEditSetting>();
            using var cmd = new OdbcCommand(sql, _connection);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a });
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new ColumnEditSetting
                {
                    TableKey = r.GetString(0),
                    ColumnKey = r.GetString(1),
                    IsEditable = Convert.ToBoolean(r.GetValue(2)),
                    UpdatedBy = r.IsDBNull(3) ? null : r.GetString(3),
                    UpdatedDate = r.IsDBNull(4) ? null : r.GetDateTime(4),
                    DisplayName = r.IsDBNull(5) ? null : r.GetString(5),
                });
            }
            return list;
        }
    }
}
