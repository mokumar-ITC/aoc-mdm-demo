using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using AOC_MDM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using AOC_MDM.Core.Entities;
using Microsoft.Extensions.Options;
using AOC_MDM.Services.DTO;
using Microsoft.Extensions.Caching.Memory;
using AOC_MDM.Services.Service;

namespace AOC_MDM_MetaData_GUI.Models
{
    public class RoleRequirementFilter : IAsyncAuthorizationFilter
    {
        private readonly int _requiredLevel;
        private readonly IUserRoleService _userRoleService;
        private readonly UserRoleSettings _userRoleSettings;
        private readonly IMemoryCache _cache;

        public RoleRequirementFilter(int requiredLevel, IUserRoleService userRoleService,
              IOptions<UserRoleSettings> userRoleSettings,
               IMemoryCache cache)
        {
            _requiredLevel = requiredLevel;
            _userRoleService = userRoleService;
            _userRoleSettings = userRoleSettings.Value;
            _cache = cache;
        }
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Logging whether role check or hardcoded role is being used
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RoleRequirementFilter>>();
            string userName = "";
            UserRoleDTO userRole;

            if (_userRoleSettings.EnableRoleCheck) // Prod server
            {
                userName = context.HttpContext.User.Identity?.Name?.Split('\\').LastOrDefault();

                if (string.IsNullOrEmpty(userName))
                {
                    // Not logged in: Forms mode redirects to /Account/Login, Windows mode sends a 401 challenge
                    context.Result = new ChallengeResult();
                    return;
                }

                logger.LogInformation($"Validating user role for: {userName}");

                // Check cache for user role
                if (!_cache.TryGetValue(userName, out userRole))
                {
                    logger.LogInformation("User role not found in cache. Fetching from service.");

                    userRole = await _userRoleService.GetUserRoleAsync(userName);

                    if (userRole != null)
                    {
                        // Set cache entry
                        _cache.Set(userName, userRole, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Cache for 30 minutes
                        });
                    }
                }
                _userRoleService.CurrentUserName = userName;
                _userRoleService.CurrentUserRole = userRole;
            }
            else
            {
                logger.LogInformation("Using hardcoded role for TESTING.");
                // Use hardcoded user role for testing
                userRole = new UserRoleDTO
                {
                    Login = _userRoleSettings.HardcodedRole.UserName,
                    LevelNo = _userRoleSettings.HardcodedRole.LevelNo
                };
              
                _userRoleService.CurrentUserName = _userRoleSettings.HardcodedRole.UserName;
                _userRoleService.CurrentUserRole = userRole;
            }

            // Authorization check
            if (userRole == null || userRole.LevelNo < _requiredLevel)
            {
                logger.LogWarning($"Access denied for {userName} (level {userRole?.LevelNo}) - requires level {_requiredLevel}");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", new { required = _requiredLevel });
                return;
            }



        }
    }
    public class RoleRequirementAttribute : Attribute, IFilterFactory
    {
        private readonly int _requiredLevel;

        public RoleRequirementAttribute(int requiredLevel)
        {
            _requiredLevel = requiredLevel;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var userRoleService = serviceProvider.GetRequiredService<IUserRoleService>();
            var userRoleSettings = serviceProvider.GetRequiredService<IOptions<UserRoleSettings>>();
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            return new RoleRequirementFilter(_requiredLevel, userRoleService, userRoleSettings, memoryCache);
        }

        public bool IsReusable => false;
    }

}
