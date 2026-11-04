using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM.Services.Service;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class AOCLevel2Controller : Controller
    {
        private IHierarchyService HierarchyService => HttpContext.RequestServices.GetRequiredService<IHierarchyService>();

        private readonly IMDMAOCLevel2Service _mdmLevel2Service;
        private readonly ILogger<AOCLevel2Controller> _logger;
        private readonly IUserRoleService _userRoleService;


        public AOCLevel2Controller(IMDMAOCLevel2Service mdmLevel2Service, ILogger<AOCLevel2Controller> logger
            , IUserRoleService userRoleService)
        {
            _mdmLevel2Service = mdmLevel2Service;
            _logger = logger;
            _userRoleService = userRoleService;
        }
        /// <summary>
        /// get list of AOC level
        /// </summary>
        /// <returns></returns>
        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: AOCLevel2Controller       
        public async Task<IActionResult> Index(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting AOCLevel2Controller Index method ");            
            if (string.IsNullOrEmpty(filterColumnName))
            {
                filterColumnName = null;
                filterValue = null;
            }

            PaginatedResult<MDMViewAOCLevel2DTO> Items = new PaginatedResult<MDMViewAOCLevel2DTO>();
            Items.Items = await _mdmLevel2Service.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);

            //Total records
            var totalRecords = Items.TotalCount;
            Items.filterValue = filterValue;
            Items.filterColumnName = filterColumnName;

            Items.selectedPageColumns = selectedPageColumns;
            dynamic model = new ExpandoObject();
            model = Items;
            _logger.LogInformation($"Ending AOCLevel2Controller Index method for Items: {Items}");

            return View(model);

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
        public async Task<IActionResult> Edit(string id, string spendArea,string team)
        {
            _logger.LogInformation($"Starting AOCLevel2Controller Edit method for id:{id}");

            AOCLevel2ViewModel levelView = new AOCLevel2ViewModel();
            levelView.TotalNoOfAffectedRecsOfItem = 0;
            levelView.TotalNoOfAffectedRecsOfVendor = 0;
            var result = await _mdmLevel2Service.GetRecsWithAffectedAttributesAsync(id);
            if (result != null)
            {
                levelView.TotalNoOfAffectedRecsOfItem = result.TotalNoOfAffectedRecsOfItem;
                levelView.TotalNoOfAffectedRecsOfVendor = result.TotalNoOfAffectedRecsOfVendor; 
            }
            var level = await _mdmLevel2Service.GetFilteredAsync(id);
            levelView.lst_Level = await _mdmLevel2Service.GetDistinctLEVEL1ActiveAsync();
            levelView.Level1 = level.Level1;
            levelView.Level2 = level.Level2;
            levelView.Team = team;
            levelView.SpendArea = spendArea;
            levelView.Enabled = level.Enabled;
            levelView.originalLevel1= level.Level1;           
            levelView.Id = id;
            _logger.LogInformation($"Ending AOCLevel2Controller Edit method for levelView:{levelView}");
            return View(levelView);
        }

        // POST: AOCLevel2Controller/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)] //3 => Approver or above (min role)
        public async Task<ActionResult> EditAsync(string Id, AOCLevel2ViewModel collection)
        {
            _logger.LogInformation($"Starting AOCLevel2Controller Edit method for id,collection:{Id},{collection}");
            try
            {
                if (string.IsNullOrEmpty(collection.Level1))
                {
                    collection.Level1 = collection.originalLevel1;
                }

                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMAOCLevel2DTO obj = new MDMAOCLevel2DTO();
                obj.UpdatedBy = username;
                obj.Level1 = collection.Level1;
                obj.IsActive = true;
                obj.Enabled = collection.Enabled;
                obj.Level2 = collection.Level2;
                var result = await _mdmLevel2Service.UpdateDirectlyAsync(Id, obj, username, "");
                if (result)
                {
                    _logger.LogInformation($"Starting AOCLevel2Controller Edit method for result:{result}");
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

        [RoleRequirement(1)]
        public async Task<IActionResult> Search(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting Level2Controller Search method for filterColumnName,filterValue:{filterColumnName},{filterValue}");
            return RedirectToAction(nameof(Index), new
            {

                filterColumnName = filterColumnName,
                filterValue,
                selectedPageColumns = selectedPageColumns,
                search
            });
        }
        [RoleRequirement(3)] //3 => Approver or above (min role)
        public async Task<ActionResult> Enable(string id)
        {
            var res = await _mdmLevel2Service.GetFilteredAsync(id);

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            // Disabling cascades to everything below (database trigger); enabling needs an enabled parent
            bool disabling = res.Enabled == "Yes";
            if (!disabling)
            {
                var blocked = await HierarchyService.CheckCanEnableAsync("level2", id);
                if (blocked != null)
                {
                    TempData["errorMessage"] = blocked;
                    return RedirectToAction(nameof(Index));
                }
            }
            res.Enabled = disabling ? "No" : "Yes";

            var result = await _mdmLevel2Service.UpdateDirectlyAsync(id, res, username, "");
            if (result)
            {
                TempData["alertMessage"] = disabling
                    ? await HierarchyService.DescribeDisableAsync("level2", id)
                    : $"\"{id}\" enabled.";
                return RedirectToAction(nameof(Index));
            }
            TempData["errorMessage"] = $"\"{id}\" could not be updated.";
            return RedirectToAction(nameof(Index));
        }

        [BindProperty]
        public List<MDMAOCLevel2DTO> Items { get; set; }
        
        public async Task<IActionResult> BulkUpdate(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting AOCLevel1Controller Index method ");
            ViewBag.lst_Level = await _mdmLevel2Service.GetDistinctLEVEL1ActiveAsync();
            PaginatedResult<MDMViewAOCLevel2DTO> Items = new PaginatedResult<MDMViewAOCLevel2DTO>();
            //Including Global Search
            Items.Items = await _mdmLevel2Service.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);
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
        public async Task<ActionResult> BulkUpdate(List<MDMAOCLevel2DTO> Items, string filterColumnName, string filterValue, string selectedPageColumns, string search)
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
                        await _mdmLevel2Service.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
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
            _logger.LogInformation($"Starting Level2 Search method for filterColumnName,filterValue: {filterColumnName},{filterValue}");

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
