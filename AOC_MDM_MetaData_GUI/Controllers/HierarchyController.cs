using AOC_MDM.Services.Interfaces;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    // Rename / delete one hierarchy node and view the hierarchy change history.
    // level: spendarea | team | level1 | level2 | level3
    public class HierarchyController : Controller
    {
        private static readonly Dictionary<string, string> ListControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["spendarea"] = "SpendArea", ["team"] = "Team", ["level1"] = "AOCLevel1", ["level2"] = "AOCLevel2", ["level3"] = "AOCLevel3"
        };

        private readonly IHierarchyService _hierarchyService;
        private readonly IUserRoleService _userRoleService;
        private readonly ILogger<HierarchyController> _logger;

        public HierarchyController(IHierarchyService hierarchyService, IUserRoleService userRoleService, ILogger<HierarchyController> logger)
        {
            _hierarchyService = hierarchyService;
            _userRoleService = userRoleService;
            _logger = logger;
        }

        [RoleRequirement(3)] //3 => Approver or above
        public async Task<IActionResult> Manage(string level, string id)
        {
            if (!ListControllers.ContainsKey(level ?? "")) return NotFound();
            var impact = await _hierarchyService.GetImpactAsync(level, id);
            if (!impact.Exists) return NotFound();
            ViewBag.ListController = ListControllers[level];
            ViewBag.RenameLocked = await HttpContext.RequestServices.GetRequiredService<IColumnEditService>().IsRenameLockedAsync(level);
            return View(impact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)]
        public async Task<IActionResult> Rename(string level, string id, string newName)
        {
            try
            {
                await _hierarchyService.RenameAsync(level, id, newName, _userRoleService.CurrentUserName);
                _logger.LogInformation("{User} renamed {Level} '{Old}' to '{New}'", _userRoleService.CurrentUserName, level, id, newName);
                TempData["alertMessage"] = $"Renamed \"{id}\" to \"{newName.Trim()}\". Everything below it now uses the new name.";
                return RedirectToAction("Index", ListControllers[level]);
            }
            catch (InvalidOperationException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return RedirectToAction(nameof(Manage), new { level, id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)]
        public async Task<IActionResult> Delete(string level, string id)
        {
            try
            {
                await _hierarchyService.DeleteAsync(level, id, _userRoleService.CurrentUserName);
                _logger.LogInformation("{User} deleted {Level} '{Name}'", _userRoleService.CurrentUserName, level, id);
                TempData["alertMessage"] = $"Deleted \"{id}\".";
                return RedirectToAction("Index", ListControllers[level]);
            }
            catch (InvalidOperationException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return RedirectToAction(nameof(Manage), new { level, id });
            }
        }

        [RoleRequirement(1)] //1 => read
        public async Task<IActionResult> History(string? levelName, string? search)
        {
            ViewBag.LevelName = levelName;
            ViewBag.Search = search;
            return View(await _hierarchyService.GetHistoryAsync(levelName, search));
        }
    }
}
