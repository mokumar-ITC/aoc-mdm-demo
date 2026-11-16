using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM.Services.Service;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Drawing.Printing;
using System.Dynamic;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class ItemSupplierRefAttriController : Controller
    {
        private readonly IMDMViewItemSupplierRefAttrsService _mdmViewItemSupplierRefAttrsService;
        private readonly IMDMItemSupplierRefAttriService _mdmItemSupplierRefAttrsService;
        private readonly ILogger<ItemSupplierRefAttriController> _logger;
        private readonly IUserRoleService _userRoleService;

        public ItemSupplierRefAttriController(IMDMViewItemSupplierRefAttrsService mdmViewItemSupplierRefAttrsService,
                                                      IMDMItemSupplierRefAttriService mdmItemSupplierRefAttrsService,
                                                      ILogger<ItemSupplierRefAttriController> logger,
                                                        IUserRoleService userRoleService)
        {
            _mdmViewItemSupplierRefAttrsService = mdmViewItemSupplierRefAttrsService;
            _mdmItemSupplierRefAttrsService = mdmItemSupplierRefAttrsService;
            _logger = logger;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: ItemSupplierRefAttri
        public async Task<ActionResult> Index(int pageNumber, int pageSize, string filterColumnName, string filterValue, string selectedPageColumns)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Index method for pageno, pagesize,filterColumnName,filterValue: {pageNumber} ,{pageSize},{filterColumnName},{filterValue}");
            if (pageNumber == 0)
            {
                pageSize = 25;
                pageNumber = 1;
                filterColumnName = null;
                filterValue = null;
            }

            PaginatedResult<MDMViewItemSupplierRefAttrsDTO> Items = await
               _mdmViewItemSupplierRefAttrsService.GetViewItemSupplierRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue);
            //Total records
            var totalRecords = Items.TotalCount;
            Items.filterValue = filterValue;
            Items.pageSize = pageSize;
            Items.pageNumber = pageNumber;
            Items.filterColumnName = filterColumnName;
            Items.fromPageNumber = Items.pageSize * Items.pageNumber;
            Items.toPageNumber = Items.fromPageNumber - Items.pageSize;
            Items.totalPages = (totalRecords / pageSize) + 1;
            Items.selectedPageColumns = selectedPageColumns;
            dynamic model = new ExpandoObject();
            model = Items;

            return View(model);
        }

        // GET: ItemSupplierRefAttri/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        [RoleRequirement(2)] //2 => Maker role (min role) 
        // GET: ItemSupplierRefAttri/Edit/5
        public async Task<ActionResult> Edit(string id, string VendorName, string ItemDescription, string ItemNo, string VendorId, string Source,string SpendArea)
        {
            var result = await _mdmItemSupplierRefAttrsService.GetFilteredAsync(id, VendorId, Source);
            //var result = Item.FirstOrDefault();
            MDMViewItemSupplierRefAttrsDTO mDMItemSupplierRefAttriDTO = new MDMViewItemSupplierRefAttrsDTO();
            mDMItemSupplierRefAttriDTO.ItemId = result.ItemId;
            mDMItemSupplierRefAttriDTO.Source = result.Source;
            mDMItemSupplierRefAttriDTO.VendorId = result.VendorId;
            mDMItemSupplierRefAttriDTO.FormulaNonFormula = result.FormulaNonFormula;
            mDMItemSupplierRefAttriDTO.RiskTransfer = result.RiskTransfer;
            mDMItemSupplierRefAttriDTO.SupplierSpecificRequirements = result.SupplierSpecificRequirements;
            mDMItemSupplierRefAttriDTO.VendorName = VendorName;
            mDMItemSupplierRefAttriDTO.ItemDescription = ItemDescription;
            mDMItemSupplierRefAttriDTO.SpendArea = SpendArea;
            mDMItemSupplierRefAttriDTO.ItemNo = ItemNo;
            return View(mDMItemSupplierRefAttriDTO);
        }

        [RoleRequirement(2)] //3 => min maker
        // POST: ItemSupplierRefAttri/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, MDMViewItemSupplierRefAttrsDTO collection)
        {
            try
            {

                bool status = false;
                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMItemSupplierRefAttriDTO mDMItemSupplierRefAttriDTO = new MDMItemSupplierRefAttriDTO();

                mDMItemSupplierRefAttriDTO.ItemId = collection.ItemId;
                mDMItemSupplierRefAttriDTO.Source = collection.Source;
                mDMItemSupplierRefAttriDTO.VendorId = collection.VendorId;
                mDMItemSupplierRefAttriDTO.FormulaNonFormula = collection.FormulaNonFormula;
                mDMItemSupplierRefAttriDTO.RiskTransfer = collection.RiskTransfer;
                mDMItemSupplierRefAttriDTO.SupplierSpecificRequirements = collection.SupplierSpecificRequirements;

                string makerId = username;

                if (role.LevelNo >= 3)
                {
                    _logger.LogInformation($"User role level is {role?.LevelNo}. Calling updateDirectly for ID: {id}");
                    status = await _mdmItemSupplierRefAttrsService.UpdateDirectlyAsync(collection.ItemId, mDMItemSupplierRefAttriDTO, true, makerId, role.LevelDesc, "");
                }
                else
                {

                    status = await _mdmItemSupplierRefAttrsService.UpdateReqByMakerAsync(collection.ItemId, mDMItemSupplierRefAttriDTO, makerId, "");
                }

                if (status)
                {
                    _logger.LogInformation($"End ItemRefAttriController Edit method for result: {status}");

                    TempData["alertMessage"] = role.LevelNo >= 3 ? "Record updated successfully" : "Your Item Supplier request is now pending approval. Please check with the approver (Checker) for updates.";
                    return RedirectToAction(nameof(Index));
                }
                else { return View(); }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied.");
                return RedirectToAction("AccessDenied", "Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during Edit for ID: {id}");
                return RedirectToAction("Handle", "Error");
            }
        }


        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Search(int pageNumber, int pageSize, string filterColumnName, string filterValue, string statusValue, string formula, string selectedPageColumns)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Search method for pageno, pagesize,filterColumnName,filterValue,formula: {pageNumber} ,{pageSize},{filterColumnName},{filterValue},{statusValue},{formula}");

            if (!string.IsNullOrEmpty(statusValue))
            {
                filterValue = statusValue;
            }
            if (!string.IsNullOrEmpty(formula))
            {
                filterValue = formula;
            }
            return RedirectToAction(nameof(Index), new
            {
                pageNumber = pageNumber,
                pageSize = pageSize,
                filterColumnName = filterColumnName,
                filterValue,
                selectedPageColumns = selectedPageColumns
            });
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> Approve(string id, string vendorId, string source)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Approve method for id: {id}");

            var result = await GetApprovalRecords(id.ToString(), vendorId, source);
            dynamic model = new ExpandoObject();
            if (result != null)
            {
                model.MainTable = result.MainTable;
                model.AuditTable = result.AuditTable;
            }
            _logger.LogInformation($"Ending ItemSupplierRefAttriController Approve method for result: {result}");

            return View(model);

        }

        [RoleRequirement(3)] //3 => approver or above only
        private async Task<MDMItemSupplierRefAttriApprovalDTO> GetApprovalRecords(string item, string vendorId, string source)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController GetApprovalRecords method for id: {item}");
            var result = await _mdmItemSupplierRefAttrsService.GetApprovalAsync(item, vendorId, source); //TODO   
            _logger.LogInformation($"Ending ItemSupplierRefAttriController GetApprovalRecords method for result: {result}");
            return result;
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> ItemSupplierAudit()
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController ItemSupplierAudit method");
            var result = await _mdmItemSupplierRefAttrsService.ItemSupplierAuditGetAsync();
            _logger.LogInformation($"Ending ItemSupplierRefAttriController ItemSupplierAudit method for result: {result}");
            return View(result);
        }

        [RoleRequirement(3)] //3 => approver or above only
        private async Task<bool> Update(int auditId, string remarks, bool status)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Update method for auditId,remarks ,status:{auditId},{remarks},{status}");

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            string checkerId = username;
            var result = await _mdmItemSupplierRefAttrsService.ApproveOrRejectUpdateAsync(auditId, status, checkerId, remarks);
            _logger.LogInformation($"Ending ItemSupplierRefAttriController Update method for result:{result}");

            return result;

        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpGet]
        public async Task<IActionResult> Reject(Int64 id)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Reject method for id:{id}");
            var result = await _mdmItemSupplierRefAttrsService.ItemSupplierAuditGetAsync(id);
            _logger.LogInformation($"Ending ItemSupplierRefAttriController Reject method for result:{result}");
            return View(result.FirstOrDefault());

        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        public async Task<IActionResult> Reject(int auditId, string Remarks)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Update method for auditId,remarks:{auditId},{Remarks}");

            var result = await Update(auditId, Remarks, false);
            if (result)
            {
                _logger.LogInformation($"Ending ItemSupplierRefAttriController Update method for result:{result}");

                TempData["alertMessage"] = "Rejected successfully !";
                return RedirectToAction(nameof(ItemSupplierAudit));
            }
            else { return View(); }
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> Approved(int id, string? remarks = null)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Approved method for Id:{id}");

            var result = await Update(id, remarks ?? "", true);
            if (result)
            {
                _logger.LogInformation($"Ending ItemSupplierRefAttriController Approved method for result:{result}");

                TempData["alertMessage"] = "Item approval successful! The item is now active in the system."
                    + await HttpContext.RequestServices.GetRequiredService<IColumnEditService>().DescribeLockedForApprovalAsync("itemsupplier");
                return RedirectToAction(nameof(ItemSupplierAudit));
            }
            else { return View(); }
        }
        public async Task<ActionResult> BulkUpdate(int pageNumber, int pageSize, string filterColumnName, string filterValue, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Index method for pageno, pagesize,filterColumnName,filterValue: {pageNumber} ,{pageSize},{filterColumnName},{filterValue}");
            if (pageNumber == 0)
            {
                pageSize = 25;
                pageNumber = 1;
                filterColumnName = null;
                filterValue = null;
            }

          
            PaginatedResult<MDMViewItemSupplierRefAttrsDTO> Items = new PaginatedResult<MDMViewItemSupplierRefAttrsDTO>();

            if (string.IsNullOrEmpty(search))
            {
                Items = await _mdmViewItemSupplierRefAttrsService.GetViewItemSupplierRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue);
            }
            else
            {
                //Including Global Search
                 Items = await _mdmViewItemSupplierRefAttrsService.GetDataPlusGlobalSearchAsync(pageNumber, pageSize, filterColumnName, filterValue, search);

            }
            //Total records
            var totalRecords = Items.TotalCount;
            Items.filterValue = filterValue;
            Items.pageSize = pageSize;
            Items.pageNumber = pageNumber;
            Items.filterColumnName = filterColumnName;
            Items.fromPageNumber = Items.pageSize * Items.pageNumber;
            Items.toPageNumber = Items.fromPageNumber - Items.pageSize;
            Items.totalPages = (totalRecords / pageSize) + 1;
            Items.selectedPageColumns = selectedPageColumns;
            Items.search = search;
            dynamic model = new ExpandoObject();
            model = Items;

            return View(model);
        }
        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> BulkUpdateSearch(int pageNumber, int pageSize, string filterColumnName, string filterValue, string statusValue, string formula, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting ItemSupplierRefAttriController Search method for pageno, pagesize,filterColumnName,filterValue,formula: {pageNumber} ,{pageSize},{filterColumnName},{filterValue},{statusValue},{formula}");

            if (!string.IsNullOrEmpty(statusValue))
            {
                filterValue = statusValue;
            }
            if (!string.IsNullOrEmpty(formula))
            {
                filterValue = statusValue;
            }
            return RedirectToAction(nameof(BulkUpdate), new
            {
                pageNumber = pageNumber,
                pageSize = pageSize,
                filterColumnName = filterColumnName,
                filterValue,
                selectedPageColumns,
                search
            });
        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkUpdate(PaginatedResult<MDMViewItemSupplierRefAttrsDTO> model)
        {
            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            if (role.LevelNo >= 3)
            {
                if (model.Items != null)
                {
                    var selectedData = model.Items.Where(x => x.IsSelected).ToList();
                    //   BulkUpdateAsync
                    if (selectedData.Count > 0)
                    {
                        await _mdmItemSupplierRefAttrsService.BulkUpdateAsync(selectedData, username, role.LevelDesc, "Bulk Update!");
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
                pageNumber = model.pageNumber,
                pageSize = model.pageSize,
                filterColumnName = model.filterColumnName,
                filterValue = model.filterValue,
                selectedPageColumns = model.selectedPageColumns,
                search=model.search
            });
        }

    }
}
