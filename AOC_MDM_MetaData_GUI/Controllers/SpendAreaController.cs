using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class SpendAreaController : Controller
    {
        private IHierarchyService HierarchyService => HttpContext.RequestServices.GetRequiredService<IHierarchyService>();

        private readonly IMDMSpendAreaService _mdmSpendAreaService;
        private readonly ILogger<SpendAreaController> _logger;
        private readonly IUserRoleService _userRoleService;

        public SpendAreaController(IMDMSpendAreaService mdmSpendAreaService, ILogger<SpendAreaController> logger,
             IUserRoleService userRoleService)
        {
            _mdmSpendAreaService = mdmSpendAreaService;
            _logger = logger;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation($"Starting SpendAreaController Index method");
            var result = await _mdmSpendAreaService.GetSpendAreaAsync();
            _logger.LogInformation($"Ending SpendAreaController Index method");
            return View(result);
        }

        // GET: SpendAreaController/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }


        [RoleRequirement(3)] //3 => Approver or above (min role)
        // GET: SpendAreaController/Edit/5
        public async Task<ActionResult> EditAsync(string id)
        {
            _logger.LogInformation($"Starting SpendAreaController EditAsync method id:{id}");

            var result = await _mdmSpendAreaService.GetFilteredAsync(id);
            _logger.LogInformation($"Ending SpendAreaController EditAsync method result:{result}");

            return View(result);
        }

        // POST: SpendAreaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)] //3 => Approver or above (min role) 
        public async Task<IActionResult> Edit(string Id, MDMSpendAreaDTO collection)
        {
            try
            {
                _logger.LogInformation($"Starting SpendAreaController Edit method id,collection:{Id},{collection}");

                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;


                MDMSpendAreaDTO obj = new MDMSpendAreaDTO();
                obj.UpdatedBy = username;
                obj.spend_area = collection.spend_area;
                obj.IsActive = true;
                obj.Enabled = collection.Enabled;

                var result = await _mdmSpendAreaService.UpdateDirectlyAsync(Id, obj, username, "");
                if (result)
                {
                    _logger.LogInformation($"Ending SpendAreaController Edit method result:{result}");

                    TempData["alertMessage"] = "Record updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View();
                }
            }
            catch
            {
                return View();
            }
        }

        [RoleRequirement(3)] //3 => Approver or above (min role)
        public async Task<ActionResult> Enable(string id)
        {
            var res = await _mdmSpendAreaService.GetFilteredAsync(id);

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            // Disabling cascades to everything below (database trigger); enabling needs an enabled parent
            bool disabling = res.Enabled == "Yes";
            if (!disabling)
            {
                var blocked = await HierarchyService.CheckCanEnableAsync("spendarea", id);
                if (blocked != null)
                {
                    TempData["errorMessage"] = blocked;
                    return RedirectToAction(nameof(Index));
                }
            }
            res.Enabled = disabling ? "No" : "Yes";

            var result = await _mdmSpendAreaService.UpdateDirectlyAsync(id, res, username, "");
            if (result)
            {
                TempData["alertMessage"] = disabling
                    ? await HierarchyService.DescribeDisableAsync("spendarea", id)
                    : $"\"{id}\" enabled.";
                return RedirectToAction(nameof(Index));
            }
            TempData["errorMessage"] = $"\"{id}\" could not be updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [RoleRequirement(1)] //1 => read (minimum role)                           
        public async Task<IActionResult> BulkUpdate(string search)
        {
            _logger.LogInformation($"Starting SpendAreaController Index method ");
            //  ViewBag.lst_Level = await _mdmLevel2Service.GetDistinctLEVEL1ActiveAsync();
          
            if (string.IsNullOrEmpty(search))
            {
                var result = await _mdmSpendAreaService.GetSpendAreaAsync();
                return View(result);
            }
            else
            {
                var result = await _mdmSpendAreaService.SearchAllColumnsAsync(search); // await _mdmSpendAreaService.SearchAllColumnsAsync(search);
                return View(result);
            }
        }
        [BindProperty]
        public List<MDMSpendAreaDTO> Items { get; set; }
        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkUpdate(List<MDMSpendAreaDTO> Items, string search)
        {
            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            if (role.LevelNo >= 3)
            {
                if (Items != null)
                {
                    var selectedData = Items.Where(x => x.IsSelected).ToList();

                    string roleDesc = role.LevelNo == 3 ? "Approver" : "Admin";
                    if (selectedData.Count > 0)
                    {
                        //   BulkUpdateAsync
                        await _mdmSpendAreaService.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
                        TempData["alertMessage"] = "Selected items updated successfully.";
                    }
                }
            }
            else
            {
                _logger.LogInformation($"Access denied. with role {role.LevelNo} with {role.LevelDesc}");
                return RedirectToAction("AccessDenied", "Error");
            }
            return RedirectToAction(nameof(BulkUpdate), new
            {
                search = search
            });

        }

    }
}
