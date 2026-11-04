using System.Data.Odbc;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;

namespace AOC_MDM.Infrastrucure.Repository
{
    // Login accounts in intranet.user_access (the role check reads intranet.databricks_fe_access_vw)
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly OdbcConnection _connection;
        private readonly string _table;

        private const string Columns = "login, display_name, levelno, leveldesc, password_hash, is_active, last_login";

        public UserAccountRepository(OdbcConnection connection, string catalog)
        {
            _connection = connection;
            _table = $"{catalog}.intranet.user_access";
        }

        public async Task<IEnumerable<UserAccount>> GetAllAsync()
        {
            var list = new List<UserAccount>();
            using var cmd = new OdbcCommand($"SELECT {Columns} FROM {_table} ORDER BY levelno DESC, login", _connection);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<UserAccount?> GetAsync(string login)
        {
            using var cmd = new OdbcCommand($"SELECT {Columns} FROM {_table} WHERE lower(login) = lower(?)", _connection);
            cmd.Parameters.Add(new OdbcParameter { Value = login ?? "" });
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task CreateAsync(UserAccount a)
        {
            await ExecAsync($"INSERT INTO {_table} (login, display_name, levelno, leveldesc, password_hash, is_active) VALUES (?, ?, ?, ?, ?, ?)",
                a.Login, a.DisplayName, a.LevelNo, a.LevelDesc, a.PasswordHash, a.IsActive);
        }

        public async Task UpdateAsync(UserAccount a)
        {
            await ExecAsync($"UPDATE {_table} SET display_name = ?, levelno = ?, leveldesc = ?, is_active = ? WHERE lower(login) = lower(?)",
                a.DisplayName, a.LevelNo, a.LevelDesc, a.IsActive, a.Login);
        }

        public Task SetPasswordAsync(string login, string passwordHash) =>
            ExecAsync($"UPDATE {_table} SET password_hash = ? WHERE lower(login) = lower(?)", passwordHash, login);

        public Task TouchLastLoginAsync(string login) =>
            ExecAsync($"UPDATE {_table} SET last_login = now() WHERE lower(login) = lower(?)", login);

        private async Task ExecAsync(string sql, params object?[] args)
        {
            using var cmd = new OdbcCommand(sql, _connection);
            foreach (var a in args) cmd.Parameters.Add(new OdbcParameter { Value = a ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        private static UserAccount Map(System.Data.Common.DbDataReader r) => new()
        {
            Login = r.GetString(0),
            DisplayName = r.IsDBNull(1) ? null : r.GetString(1),
            LevelNo = Convert.ToInt32(r.GetValue(2)),
            LevelDesc = r.IsDBNull(3) ? null : r.GetString(3),
            PasswordHash = r.IsDBNull(4) ? null : r.GetString(4),
            IsActive = !r.IsDBNull(5) && Convert.ToBoolean(r.GetValue(5)),
            LastLogin = r.IsDBNull(6) ? null : r.GetDateTime(6),
        };
    }
}
