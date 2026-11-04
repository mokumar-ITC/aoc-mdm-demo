using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM.Services.Service;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Printing;
using System.Dynamic;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class AOCLevel3Controller : Controller
    {
        private IHierarchyService HierarchyService => HttpContext.RequestServices.GetRequiredService<IHierarchyService>();

        private readonly IMDMAOCLevel3Service _mdmLevel3Service;
        private readonly IMDMAOCLevel2Service _mdmLevel2Service;
        private readonly ILogger<AOCLevel3Controller> _logger;
        private readonly IUserRoleService _userRoleService;

        public AOCLevel3Controller(IMDMAOCLevel3Service mdmLevel3Service, IMDMAOCLevel2Service mdmLevel2Service,
            ILogger<AOCLevel3Controller> logger
            , IUserRoleService userRoleService)
        {
            _mdmLevel3Service = mdmLevel3Service;
            _mdmLevel2Service = mdmLevel2Service;
            _logger = logger;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: AOCLevel2Controller
        public async Task<IActionResult> Index(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            //_logger.LogInformation($"Starting AOCLevel3Controller Index method ");
            //var Team = await _mdmLevel3Service.GetAOCLevel3sAsync();
            //_logger.LogInformation($"Ending AOCLevel3Controller Index method for result count :{Team.Count()}");
            //return View(Team);

            _logger.LogInformation($"Starting AOCLevel3Controller Index method for filterColumnName,filterValue: {filterColumnName},{filterValue}");

            if (string.IsNullOrEmpty(filterColumnName))
            {
                filterColumnName = null;
                filterValue = null;
            }

            PaginatedResult<MDMViewAOCLevel3DTO> Items = new PaginatedResult<MDMViewAOCLevel3DTO>();
            Items.Items = await _mdmLevel3Service.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);

            //Total records
            var totalRecords = Items.TotalCount;
            Items.filterValue = filterValue;
            Items.filterColumnName = filterColumnName;

            Items.selectedPageColumns = selectedPageColumns;
            dynamic model = new ExpandoObject();
            model = Items;
            _logger.LogInformation($"Ending ItemRefAttrsController Index method for Items: {Items}");

            return View(model);
        }

        [RoleRequirement(1)]
        public async Task<IActionResult> Search(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting Level3Controller Search method for filterColumnName,filterValue:{filterColumnName},{filterValue}");
            return RedirectToAction(nameof(Index), new
            {

                filterColumnName = filterColumnName,
                filterValue,
                selectedPageColumns = selectedPageColumns,
                search
            });
        }

        // GET: AOCLevel2Controller/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AOCLevel2Controller/Create
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
        // GET: AOCLevel2Controller/Edit/5
        public async Task<IActionResult> Edit(string Id,string level1,string spendArea,string team)
        {
            _logger.LogInformation($"Starting AOCLevel3Controller Edit method for Id:{Id}");
            AOCLevel3ViewModel levelView = new AOCLevel3ViewModel();
            var count = await _mdmLevel3Service.GetRecsWithAffectedAttributesAsync(Id);
            levelView.TotalNoOfAffectedRecsOfVendor = count.TotalNoOfAffectedRecsOfVendor;
            levelView.TotalNoOfAffectedRecsOfItem = count.TotalNoOfAffectedRecsOfItem;
            var level = await _mdmLevel3Service.GetFilteredAsync(Id);
            levelView.lst_Level = await _mdmLevel2Service.GetDistinctLEVEL2ActiveAsync();
            levelView.Level3 = level.Level3;
            levelView.originalLevel2 = level.Level2;
            levelView.Level2 = level.Level2;
            levelView.Enabled = level.Enabled;
            levelView.Level1 = level1;
            levelView.SpendArea = spendArea;
            levelView.Team = team;
            levelView.Id = Id;
            levelView.Feedstock1 = level.Feedstock1;
            levelView.Feedstock2 = level.Feedstock2;
            levelView.Feedstock3 = level.Feedstock3;
            levelView.ConflictMineralForcedLaborRegCheck = level.ConflictMineralForcedLaborRegCheck;
            levelView.Hazardous = level.Hazardous;
            _logger.LogInformation($"Ending AOCLevel3Controller Edit method for levelView:{levelView}");
            return View(levelView);
        }

        // POST: AOCLevel2Controller/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)] //3 => Approver or above (min role)
        public async Task<ActionResult> EditAsync(string Id, AOCLevel3ViewModel collection)
        {
            try
            {
                _logger.LogInformation($"Starting AOCLevel3Controller EditAsync method for Id,collection:{Id},{collection}");

                if (string.IsNullOrEmpty(collection.Level2))
                {
                    collection.Level2 = collection.originalLevel2;
                }

                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMAOCLevel3DTO obj = new MDMAOCLevel3DTO();
                obj.UpdatedBy = "Ankit";
                obj.Level3 = collection.Level3;
                obj.IsActive = true;
                obj.Enabled = collection.Enabled;
                obj.Level2 = collection.Level2;
                obj.Feedstock1 = collection.Feedstock1;
                obj.Feedstock2 = collection.Feedstock2;
                obj.Feedstock3 = collection.Feedstock3;
                obj.ConflictMineralForcedLaborRegCheck = collection.ConflictMineralForcedLaborRegCheck;
                obj.Hazardous = collection.Hazardous;
                var result = await _mdmLevel3Service.UpdateDirectlyAsync(Id, obj, username, "");
                if (result)
                {
                    _logger.LogInformation($"Ending AOCLevel3Controller EditAsync method for result:{result}");

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
            var res = await _mdmLevel3Service.GetFilteredAsync(id);

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            // Disabling cascades to everything below (database trigger); enabling needs an enabled parent
            bool disabling = res.Enabled == "Yes";
            if (!disabling)
            {
                var blocked = await HierarchyService.CheckCanEnableAsync("level3", id);
                if (blocked != null)
                {
                    TempData["errorMessage"] = blocked;
                    return RedirectToAction(nameof(Index));
                }
            }
            res.Enabled = disabling ? "No" : "Yes";

            var result = await _mdmLevel3Service.UpdateDirectlyAsync(id, res, username, "");
            if (result)
            {
                TempData["alertMessage"] = disabling
                    ? await HierarchyService.DescribeDisableAsync("level3", id)
                    : $"\"{id}\" enabled.";
                return RedirectToAction(nameof(Index));
            }
            TempData["errorMessage"] = $"\"{id}\" could not be updated.";
            return RedirectToAction(nameof(Index));
        }
        
        [BindProperty]
        public List<MDMAOCLevel3DTO> Items { get; set; }
        [HttpGet]
        [RoleRequirement(1)]
        public async Task<IActionResult> BulkUpdate(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting AOCLevel1Controller Index method ");
            ViewBag.lst_Level = await _mdmLevel2Service.GetDistinctLEVEL2ActiveAsync();
            PaginatedResult<MDMViewAOCLevel3DTO> Items = new PaginatedResult<MDMViewAOCLevel3DTO>();
            //Including Global Search
            Items.Items = await _mdmLevel3Service.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);
            //Total records
            var totalRecords = Items.TotalCount;
            Items.filterValue = filterValue;
            Items.filterColumnName = filterColumnName;
            Items.selectedPageColumns = selectedPageColumns;
            Items.search = search;
            dynamic model = new ExpandoObject();
            model = Items;

            return View(model);
        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkUpdate(List<MDMAOCLevel3DTO> Items, string filterColumnName, string filterValue, string selectedPageColumns, string search)
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
                        await _mdmLevel3Service.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
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
                filterColumnName,
                filterValue,
                selectedPageColumns,
                search = search
            });

        }
        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> BulkUpdateSearch(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting Level3 Search method for filterColumnName,filterValue: {filterColumnName},{filterValue}");

            return RedirectToAction(nameof(BulkUpdate), new
            {
                filterColumnName = filterColumnName,
                filterValue,
                selectedPageColumns,
                search
            });
        }

    }
}
