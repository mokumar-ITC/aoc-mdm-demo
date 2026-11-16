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
    public class AOCLevel1Controller : Controller
    {
        private IHierarchyService HierarchyService => HttpContext.RequestServices.GetRequiredService<IHierarchyService>();
        private IColumnEditService ColumnEdit => HttpContext.RequestServices.GetRequiredService<IColumnEditService>();

        private readonly IMDMAOCLevel1Service _mdmLevel1Service;
        private readonly IMDMTeamService _mdmTeamService;
        private readonly ILogger<AOCLevel1Controller> _logger;
        private readonly IUserRoleService _userRoleService;

        public AOCLevel1Controller(IMDMAOCLevel1Service mdmLevel1Service, IMDMTeamService mdmTeamService,
            ILogger<AOCLevel1Controller> logger,
              IUserRoleService userRoleService)
        {
            _mdmLevel1Service = mdmLevel1Service;
            _mdmTeamService = mdmTeamService;   
            _logger = logger;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: AOCLevel1Controller
        //public async Task<IActionResult> Index()
        //{
        //    _logger.LogInformation($"Starting AOCLevel1Controller Index method");
        //    var Team = await _mdmLevel1Service.GetViewAOCLevel1sAsync(); //GetViewAOCLevel1sAsync()
        //    _logger.LogInformation($"Ending AOCLevel1Controller Index method for Result count:{Team.Count()}");
        //    return View(Team);
        //}
        public async Task<ActionResult> Index(string filterColumnName, string filterValue, string selectedPageColumns,string search)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Index method for filterColumnName,filterValue: {filterColumnName},{filterValue}");

            if (string.IsNullOrEmpty(filterColumnName))
            {                
                filterColumnName = null;
                filterValue = null;
            }

            PaginatedResult<MDMViewAOCLevel1DTO> Items  =new  PaginatedResult<MDMViewAOCLevel1DTO>();
            Items.Items = await _mdmLevel1Service.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);


            //PaginatedResult<MDMViewItemRefAttrsDTO> Items = await
            //    _mdmItemRefAttrsService.GetViewItemRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue);
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

        // GET: AOCLevel1Controller/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AOCLevel1Controller/Create
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
        // GET: AOCLevel1Controller/Edit/5
        public async Task<IActionResult> Edit(string id,string SpendArea)
        {
            _logger.LogInformation($"Starting AOCLevel1Controller Edit method from id:{id}");
            var level = await _mdmLevel1Service.GetFilteredAsync(id);
            AOCLevel1ViewModel levelView = new AOCLevel1ViewModel();
            levelView.lst_Team = await _mdmTeamService.GetDistinctTeamsActiveAsync();
            levelView.Level1 = level.Level1;
            levelView.SpendArea = SpendArea;
            levelView.Team = level.Team;
            levelView.Enabled = level.Enabled;
            levelView.Id = id;
            levelView.originalTeam = level.Team;
            levelView.TotalNoOfAffectedRecsOfItem = 0;
            levelView.TotalNoOfAffectedRecsOfVendor = 0;
            _logger.LogInformation($"Ending AOCLevel1Controller Edit method from levelView:{levelView}");
            return View(levelView);
        }

        [RoleRequirement(3)] //3 => Approver or above (min role)
        // POST: AOCLevel1Controller/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string Id, AOCLevel1ViewModel collection)
        {
            try
            {
                _logger.LogInformation($"Starting AOCLevel1Controller Edit method from Id,collection:{Id},{collection}");

                if (string.IsNullOrEmpty(collection.Team))
                {
                    collection.Team = collection.originalTeam;
                }

                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMAOCLevel1DTO obj = new MDMAOCLevel1DTO();
                obj.UpdatedBy = username;
                obj.Team = collection.Team;
                obj.IsActive = true;
                obj.Enabled =collection.Enabled;
                obj.Level1 = collection.Level1;
                var result = await _mdmLevel1Service.UpdateDirectlyAsync(Id, obj, username, "");
                if (result)
                {
                    _logger.LogInformation($"Ending AOCLevel1Controller Edit method from result:{result}");

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

        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Search(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting Level1Controller Search method for filterColumnName,filterValue:{filterColumnName},{filterValue}");
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
            if (await ColumnEdit.IsLockedAsync("level1", "Enabled"))
            {
                TempData["errorMessage"] = "The Enabled column is locked by an administrator (Administration > Column Editing).";
                return RedirectToAction(nameof(Index));
            }
            
            var res = await _mdmLevel1Service.GetFilteredAsync(id);

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            // Disabling cascades to everything below (database trigger); enabling needs an enabled parent
            bool disabling = res.Enabled == "Yes";
            if (!disabling)
            {
                var blocked = await HierarchyService.CheckCanEnableAsync("level1", id);
                if (blocked != null)
                {
                    TempData["errorMessage"] = blocked;
                    return RedirectToAction(nameof(Index));
                }
            }
            res.Enabled = disabling ? "No" : "Yes";

            var result = await _mdmLevel1Service.UpdateDirectlyAsync(id, res, username, "");
            if (result)
            {
                TempData["alertMessage"] = disabling
                    ? await HierarchyService.DescribeDisableAsync("level1", id)
                    : $"\"{id}\" enabled.";
                return RedirectToAction(nameof(Index));
            }
            TempData["errorMessage"] = $"\"{id}\" could not be updated.";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: AOCLevel1Controller
        public async Task<IActionResult> BulkUpdate(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting AOCLevel1Controller Index method ");
            ViewBag.Team = await _mdmTeamService.GetDistinctTeamsActiveAsync();
            PaginatedResult<MDMViewAOCLevel1DTO> Items = new PaginatedResult<MDMViewAOCLevel1DTO>();
            //Including Global Search
            Items.Items = await _mdmLevel1Service.GetDataWithOrWithoutFilter(filterColumnName, filterValue,search);            
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
        [BindProperty]
        public List<MDMAOCLevel1DTO> Items { get; set; }
        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkUpdate(List<MDMAOCLevel1DTO> Items, string filterColumnName, string filterValue, string selectedPageColumns, string search)
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
                        await _mdmLevel1Service.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
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
        public async Task<IActionResult> BulkUpdateSearch(string filterColumnName, string filterValue,  string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting Level1 Search method for filterColumnName,filterValue: {filterColumnName},{filterValue}");
           
            if (search== "undefined")
            {
                search = null;
            }
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
