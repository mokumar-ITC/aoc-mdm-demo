using AOC_MDM.Services.Interfaces;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    // User and access management - Admin (level 4) only
    public class UsersController : Controller
    {
        private readonly IUserAccountService _userAccountService;
        private readonly IUserRoleService _userRoleService;
        private readonly IMemoryCache _cache;

        public UsersController(IUserAccountService userAccountService, IUserRoleService userRoleService, IMemoryCache cache)
        {
            _userAccountService = userAccountService;
            _userRoleService = userRoleService;
            _cache = cache;
        }

        [RoleRequirement(4)]
        public async Task<IActionResult> Index()
        {
            return View(await _userAccountService.GetAllAsync());
        }

        [RoleRequirement(4)]
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id)) return View(new UserEditViewModel { IsNew = true, LevelNo = 1, IsActive = true });

            var account = await _userAccountService.GetAsync(id);
            if (account == null) return NotFound();
            return View(new UserEditViewModel
            {
                Login = account.Login,
                DisplayName = account.DisplayName,
                LevelNo = account.LevelNo,
                IsActive = account.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(4)]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            try
            {
                if (model.IsNew)
                {
                    await _userAccountService.CreateAsync(model.Login, model.DisplayName, model.LevelNo, model.Password);
                    TempData["alertMessage"] = $"User \"{model.Login}\" created.";
                }
                else
                {
                    if (string.Equals(model.Login, _userRoleService.CurrentUserName, StringComparison.OrdinalIgnoreCase)
                        && (model.LevelNo < 4 || !model.IsActive))
                        throw new InvalidOperationException("You cannot remove your own Admin access or disable your own account.");

                    await _userAccountService.UpdateAsync(model.Login, model.DisplayName, model.LevelNo, model.IsActive, model.Password);
                    TempData["alertMessage"] = $"User \"{model.Login}\" updated.";
                }
                _cache.Remove(model.Login); // role change applies on the user's next request
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
    }
}
