using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using AOC_MDM.Services.DTO;
using Microsoft.Extensions.Caching.Memory;
using AOC_MDM.Core.Entities;
using Microsoft.Extensions.Options;

namespace AOC_MDM_MetaData_GUI.Models
{
    public class RoleCacheHelper
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RoleCacheHelper> _logger;
        private readonly UserRoleSettings _userRoleSettings;

        public RoleCacheHelper(IMemoryCache cache, ILogger<RoleCacheHelper> logger, IOptions<UserRoleSettings> userRoleSettings)
        {
            _cache = cache;
            _logger = logger;
            _userRoleSettings = userRoleSettings.Value;
        }

        public UserRoleDTO GetCachedUserRole(HttpContext httpContext, out bool roleFound)
        {
            roleFound = false;
            string userName;

            // Check if Role Check is enabled
            if (_userRoleSettings.EnableRoleCheck) // Production server
            {
                // Get authenticated user's name
                userName = httpContext.User.Identity?.Name?.Split('\\').LastOrDefault();

                if (string.IsNullOrEmpty(userName))
                {
                    _logger.LogError("User not logged in. Cannot validate role.");
                    throw new UnauthorizedAccessException("User not logged in.");
                }

                _logger.LogInformation($"Validating user role for: {userName}");

                // Check cache for the role
                if (_cache.TryGetValue(userName, out UserRoleDTO cachedRole))
                {
                    _logger.LogInformation($"Cached role found for user: {userName}.");
                    roleFound = true;
                    return cachedRole;
                }

                _logger.LogWarning($"No cached role found for user: {userName}.");
                return null; // Or handle fallback logic if needed
            }
            else // Testing environment
            {
                _logger.LogInformation("Using hardcoded role for testing.");

                // Return hardcoded role from configuration
                var hardcodedRole = new UserRoleDTO
                {
                    Login = _userRoleSettings.HardcodedRole.UserName,
                    LevelNo = _userRoleSettings.HardcodedRole.LevelNo
                };

                _logger.LogInformation($"Hardcoded role: {hardcodedRole.Login}, Level: {hardcodedRole.LevelNo}.");
                roleFound = true;
                return hardcodedRole;
            }
        }
    }

}
