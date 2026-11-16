using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM.Services.Service;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Dynamic;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class TeamController : Controller
    {
        private IHierarchyService HierarchyService => HttpContext.RequestServices.GetRequiredService<IHierarchyService>();
        private IColumnEditService ColumnEdit => HttpContext.RequestServices.GetRequiredService<IColumnEditService>();

        private readonly IMDMTeamService _mdmTeamService;
        private readonly IMDMSpendAreaService _mdmSpendAreaService;
        private readonly ILogger<TeamController> _logger;
        private readonly IUserRoleService _userRoleService;


        public TeamController(IMDMTeamService mdmTeamService, IMDMSpendAreaService mdmSpendAreaService,
            ILogger<TeamController> logger,
            IUserRoleService userRoleService)
        {
            _mdmTeamService = mdmTeamService;
            _mdmSpendAreaService = mdmSpendAreaService;
            _logger = logger;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: TeamController
         public async Task<ActionResult> Index(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting TeamController Index method for filterColumnName,filterValue: {filterColumnName},{filterValue}");

            if (string.IsNullOrEmpty(filterColumnName))
            {
                filterColumnName = null;
                filterValue = null;
            }

            PaginatedResult<MDMTeamDTO> Items = new PaginatedResult<MDMTeamDTO>();
            Items.Items = await _mdmTeamService.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);


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
        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Search(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting TeamController Search method for filterColumnName,filterValue:{filterColumnName},{filterValue}");
            return RedirectToAction(nameof(Index), new
            {

                filterColumnName = filterColumnName,
                filterValue,
                selectedPageColumns = selectedPageColumns,
                search
            });
        }

        // GET: TeamController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TeamController/Create
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
        // GET: TeamController/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            _logger.LogInformation($"Starting TeamController Edit method id:{id}");

            var team = await _mdmTeamService.GetFilteredAsync(id);
            TeamViewModel teamView = new TeamViewModel();
            //var team = Team.FirstOrDefault();
            teamView.lst_Team = await _mdmSpendAreaService.GetSpendAreaActiveAsync();
            teamView.SpendArea = team.SpendArea;
            teamView.originalSpendArea = team.SpendArea;
            teamView.Team = team.Team;
            teamView.Enabled = team.Enabled;
            _logger.LogInformation($"Ending TeamController Edit method teamView:{teamView}");

            return View(teamView);
        }

        [RoleRequirement(3)] //3 => Approver or above (min role)
        public async Task<ActionResult> Enable(string id)
        {
            if (await ColumnEdit.IsLockedAsync("team", "Enabled"))
            {
                TempData["errorMessage"] = "The Enabled column is locked by an administrator (Administration > Column Editing).";
                return RedirectToAction(nameof(Index));
            }
            
            var res = await _mdmTeamService.GetFilteredAsync(id);

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            // Disabling cascades to everything below (database trigger); enabling needs an enabled parent
            bool disabling = res.Enabled == "Yes";
            if (!disabling)
            {
                var blocked = await HierarchyService.CheckCanEnableAsync("team", id);
                if (blocked != null)
                {
                    TempData["errorMessage"] = blocked;
                    return RedirectToAction(nameof(Index));
                }
            }
            res.Enabled = disabling ? "No" : "Yes";

            var result = await _mdmTeamService.UpdateDirectlyAsync(id, res, username, "");
            if (result)
            {
                TempData["alertMessage"] = disabling
                    ? await HierarchyService.DescribeDisableAsync("team", id)
                    : $"\"{id}\" enabled.";
                return RedirectToAction(nameof(Index));
            }
            TempData["errorMessage"] = $"\"{id}\" could not be updated.";
            return RedirectToAction(nameof(Index));
        }
        // POST: TeamController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)] //3 => Approver or above (min role)
        public async Task<ActionResult> EditAsync(string Id, TeamViewModel collection)
        {
            try
            {
                _logger.LogInformation($"Starting TeamController Edit method id,collection:{Id},{collection}");
                if (string.IsNullOrEmpty(collection.SpendArea))
                {
                    collection.SpendArea = collection.originalSpendArea;
                }

                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMTeamDTO obj = new MDMTeamDTO();
                obj.UpdatedBy = username;
                obj.SpendArea = collection.SpendArea;
                obj.Team = collection.Team;
                obj.IsActive = true;
                obj.Enabled = collection.Enabled;

                var result = await _mdmTeamService.UpdateDirectlyAsync(Id, obj, username, "");
                if (result)
                {
                    _logger.LogInformation($"Ending TeamController Edit method result:{result}");
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

         [BindProperty]
        public List<MDMTeamDTO> Items { get; set; }
       
        public async Task<IActionResult> BulkUpdate(string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting AOCLevel1Controller Index method ");
            ViewBag.lst_SpendArea = await _mdmSpendAreaService.GetSpendAreaActiveAsync();
            PaginatedResult<MDMTeamDTO> Items = new PaginatedResult<MDMTeamDTO>();
            //Including Global Search
            Items.Items = await _mdmTeamService.GetDataWithOrWithoutFilter(filterColumnName, filterValue, search);
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
        public async Task<ActionResult> BulkUpdate(List<MDMTeamDTO> Items, string filterColumnName, string filterValue, string selectedPageColumns, string search)
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
                        await _mdmTeamService.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
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
            _logger.LogInformation($"Starting Level1 Search method for filterColumnName,filterValue: {filterColumnName},{filterValue}");

            if (search == "undefined")
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
