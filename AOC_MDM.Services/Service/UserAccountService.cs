using System.Security.Cryptography;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.Interfaces;

namespace AOC_MDM.Services.Service
{
    // PBKDF2-SHA256 password hashes stored as "PBKDF2$<iterations>$<salt b64>$<hash b64>"
    public static class PasswordHasher
    {
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string? stored)
        {
            var parts = stored?.Split('$');
            if (parts is not { Length: 4 } || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password ?? "", salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }

    public class UserAccountService : IUserAccountService
    {
        public static readonly IReadOnlyDictionary<int, string> LevelNames = new Dictionary<int, string>
        {
            [1] = "Viewer", [2] = "Maker", [3] = "Checker", [4] = "Admin"
        };

        private readonly IUnitOfWork _unitOfWork;

        public UserAccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserAccount?> ValidateAsync(string login, string password)
        {
            var account = await _unitOfWork.UserAccountRepository.GetAsync(login?.Trim() ?? "");
            if (account == null || !account.IsActive || !PasswordHasher.Verify(password, account.PasswordHash))
                return null;
            await _unitOfWork.UserAccountRepository.TouchLastLoginAsync(account.Login);
            return account;
        }

        public Task<IEnumerable<UserAccount>> GetAllAsync() => _unitOfWork.UserAccountRepository.GetAllAsync();

        public Task<UserAccount?> GetAsync(string login) => _unitOfWork.UserAccountRepository.GetAsync(login);

        public async Task CreateAsync(string login, string displayName, int levelNo, string password)
        {
            login = login?.Trim() ?? "";
            if (login.Length == 0) throw new InvalidOperationException("Login is required.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) throw new InvalidOperationException("Password must be at least 8 characters.");
            if (await _unitOfWork.UserAccountRepository.GetAsync(login) != null) throw new InvalidOperationException($"User \"{login}\" already exists.");

            await _unitOfWork.UserAccountRepository.CreateAsync(new UserAccount
            {
                Login = login,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? login : displayName.Trim(),
                LevelNo = ValidLevel(levelNo),
                LevelDesc = LevelNames[ValidLevel(levelNo)],
                PasswordHash = PasswordHasher.Hash(password),
                IsActive = true
            });
        }

        public async Task UpdateAsync(string login, string displayName, int levelNo, bool isActive, string? newPassword)
        {
            var account = await _unitOfWork.UserAccountRepository.GetAsync(login)
                          ?? throw new InvalidOperationException($"User \"{login}\" was not found.");
            account.DisplayName = string.IsNullOrWhiteSpace(displayName) ? account.Login : displayName.Trim();
            account.LevelNo = ValidLevel(levelNo);
            account.LevelDesc = LevelNames[account.LevelNo];
            account.IsActive = isActive;
            await _unitOfWork.UserAccountRepository.UpdateAsync(account);

            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword.Length < 8) throw new InvalidOperationException("Password must be at least 8 characters.");
                await _unitOfWork.UserAccountRepository.SetPasswordAsync(account.Login, PasswordHasher.Hash(newPassword));
            }
        }

        private static int ValidLevel(int levelNo) =>
            LevelNames.ContainsKey(levelNo) ? levelNo : throw new InvalidOperationException("Access level must be 1 to 4.");
    }

    public class HierarchyService : IHierarchyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public HierarchyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<HierarchyImpact> GetImpactAsync(string level, string name) => _unitOfWork.HierarchyRepository.GetImpactAsync(level, name);

        public Task RenameAsync(string level, string oldName, string newName, string userName) =>
            _unitOfWork.HierarchyRepository.RenameAsync(level, oldName, newName, userName);

        public Task DeleteAsync(string level, string name, string userName) =>
            _unitOfWork.HierarchyRepository.DeleteAsync(level, name, userName);

        public Task<IEnumerable<HierarchyAuditEntry>> GetHistoryAsync(string? levelLabel, string? search, int limit = 500) =>
            _unitOfWork.HierarchyRepository.GetHistoryAsync(levelLabel, search, limit);

        // Returns an error message when the node can't be enabled because its parent is disabled, otherwise null
        public async Task<string?> CheckCanEnableAsync(string level, string name)
        {
            var impact = await GetImpactAsync(level, name);
            return impact.ParentEnabled == "No"
                ? $"Cannot enable \"{name}\": its parent {impact.ParentLabel} \"{impact.ParentName}\" is disabled. Enable the parent first."
                : null;
        }

        // Message shown after a disable, describing what was disabled with it
        public async Task<string> DescribeDisableAsync(string level, string name)
        {
            var impact = await GetImpactAsync(level, name);
            return impact.TotalBelow == 0
                ? $"\"{name}\" disabled."
                : $"\"{name}\" disabled, together with {impact.TotalBelow} level(s) below it.";
        }
    }
}
