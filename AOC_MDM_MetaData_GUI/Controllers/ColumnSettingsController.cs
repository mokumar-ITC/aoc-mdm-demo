using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    // Which columns can be edited on each table - Admin (level 4) only
    public class ColumnSettingsController : Controller
    {
        private readonly IColumnEditService _columnEdit;
        private readonly IUserRoleService _userRoleService;

        public ColumnSettingsController(IColumnEditService columnEdit, IUserRoleService userRoleService)
        {
            _columnEdit = columnEdit;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(4)]
        public async Task<IActionResult> Index()
        {
            return View(await _columnEdit.GetStatusAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(4)]
        public async Task<IActionResult> Save(string tableKey, List<string> editable)
        {
            var table = ColumnEditCatalog.Find(tableKey);
            if (table == null) return NotFound();

            var changed = await _columnEdit.SaveAsync(table.Key, editable, _userRoleService.CurrentUserName);
            TempData["alertMessage"] = changed == 0
                ? $"{table.Label}: no changes to save."
                : $"{table.Label}: {changed} column(s) updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
