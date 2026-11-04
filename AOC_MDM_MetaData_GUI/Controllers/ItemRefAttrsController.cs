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
    public class ItemRefAttrsController : Controller
    {
        private readonly MDMViewItemRefAttrsService _mdmItemRefAttrsService;
        private readonly IMDMItemRefAttrsService _msmItemRefAttrsService;
        private readonly IMDMAOCLevel3Service _mdmLevel3Service;
        private readonly ILogger<ItemSupplierRefAttriController> _logger;
        private readonly IUserRoleService _userRoleService;

        public ItemRefAttrsController(MDMViewItemRefAttrsService mdmItemRefAttrsService,
            IMDMAOCLevel3Service mdmLevel3Service, IMDMItemRefAttrsService msmItemRefAttrsService,
            ILogger<ItemSupplierRefAttriController> logger, IUserRoleService userRoleService)
        {
            _mdmItemRefAttrsService = mdmItemRefAttrsService;
            _mdmLevel3Service = mdmLevel3Service;
            _msmItemRefAttrsService = msmItemRefAttrsService;
            _logger = logger;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        // GET: ItemRefAttrsController
        public async Task<ActionResult> Index(int pageNumber, int pageSize, string filterColumnName, string filterValue, string selectedPageColumns)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Index method for pageNumber, pagesize,filterColumnName,filterValue: {pageNumber} ,{pageSize},{filterColumnName},{filterValue}");

            if (pageNumber == 0)
            {
                pageSize = 25;
                pageNumber = 1;
                filterColumnName = null;
                filterValue = null;
            }

            //sortColumn is null, means UpdateDate and SortAcending is null, means descending
            PaginatedResult<MDMViewItemRefAttrsDTO> Items = await
            //    _mdmItemRefAttrsService.GetViewItemRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue, null, false);
                _mdmItemRefAttrsService.GetViewItemRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue, "UpdatedDate", false);
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
            _logger.LogInformation($"Ending ItemRefAttrsController Index method for Items: {Items}");

            return View(model);
        }


        [RoleRequirement(2)] //2 => Maker role (min role) 
        // GET: ItemRefAttrsController/Edit/5
        public async Task<ActionResult> Edit(string id, string ItemNo, string ItemDescription, string Source)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Edit method for id: {id}");
            var result = await _msmItemRefAttrsService.GetFilteredAsync(id, Source);
            ViewBag.ItemEnglishName = await _msmItemRefAttrsService.GetDistinctColumnValuesAsync("item_english_name");
            ViewBag.CasNumber = await _msmItemRefAttrsService.GetDistinctColumnValuesAsync("cas_number");
            ViewBag.lstLevel3 = await _mdmLevel3Service.GetDistinctLEVEL3ActiveAsync();
            _logger.LogInformation($"Ending ItemRefAttrsController Edit method for id: {result}");
            MDMViewItemRefAttrsDTO mDMViewItem = new MDMViewItemRefAttrsDTO();
            mDMViewItem.AddressableSpend = result.AddressableSpend;
            mDMViewItem.AddressableSpend = result.AddressableSpend;
            mDMViewItem.Source = result.Source;
            mDMViewItem.Remarks = result.Remarks;
            mDMViewItem.Level3 = result.Level3;
            mDMViewItem.CasNumber = result.CasNumber;
            mDMViewItem.CurrentWavePlan = result.CurrentWavePlan;
            mDMViewItem.ItemEnglishName = result.ItemEnglishName;
            mDMViewItem.ItemForm = result.ItemForm;
            mDMViewItem.ItemId = result.ItemId;
            mDMViewItem.MaterialCriticalityBasedOnRevenue = result.MaterialCriticalityBasedOnRevenue;
            mDMViewItem.ItemNo = ItemNo;
            mDMViewItem.ItemDescription = ItemDescription;
            return View(mDMViewItem);
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Search(int pageNumber, int pageSize, string filterColumnName, string filterValue, string statusValue, string formula, string selectedPageColumns)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Search method for pageno, pagesize,filterColumnName,filterValue: {pageNumber} ,{pageSize},{filterColumnName},{filterValue},{statusValue}");

            if (!string.IsNullOrEmpty(statusValue))
            {
                filterValue = statusValue;
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

        // POST: ItemRefAttrsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(2)] //2 => maker role (min.)
        public async Task<ActionResult> Edit(string id, MDMItemRefAttributesAudit collection)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Edit method for id, collection: {id} ,{collection}");
            try
            {
                bool status = false;
                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMItemRefAttriDTO mDMItemrRefAttriDTO = new MDMItemRefAttriDTO();
                mDMItemrRefAttriDTO.AddressableSpend = collection.AddressableSpend;
                mDMItemrRefAttriDTO.Source = collection.Source;
                mDMItemrRefAttriDTO.Remarks = collection.Remarks;
                mDMItemrRefAttriDTO.Level3 = collection.Level3;
                mDMItemrRefAttriDTO.CasNumber = collection.CasNumber;
                mDMItemrRefAttriDTO.CurrentWavePlan = collection.CurrentWavePlan;
                mDMItemrRefAttriDTO.ItemEnglishName = collection.ItemEnglishName;
                mDMItemrRefAttriDTO.ItemForm = collection.ItemForm;
                mDMItemrRefAttriDTO.ItemId = collection.ItemId;
                mDMItemrRefAttriDTO.MaterialCriticalityBasedOnRevenue = collection.MaterialCriticalityBasedOnRevenue;

                string makerId = _userRoleService.CurrentUserName;

                if (role.LevelNo >= 3)
                {
                    status = await _msmItemRefAttrsService.UpdateDirectlyAsync(collection.ItemId, mDMItemrRefAttriDTO, true, makerId, role.LevelDesc, "");
                }
                else
                {

                    status = await _msmItemRefAttrsService.UpdateReqByMakerAsync(collection.ItemId, mDMItemrRefAttriDTO, makerId, "");
                }

                if (status)
                {
                    _logger.LogInformation($"End ItemRefAttriController Edit method for result: {status}");

                    TempData["alertMessage"] = role.LevelNo >= 3 ? "Record updated successfully" : "Your Item request is now pending approval. Please check with the approver (Checker) for updates.";
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

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> Approve(string id, string source)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Approve method for id: {id}");

            var result = await GetApprovalRecords(id, source);
            dynamic model = new ExpandoObject();
            if (result != null)
            {
                model.MainTable = result.MainTable;
                model.AuditTable = result.AuditTable;
            }
            _logger.LogInformation($"Ending ItemRefAttrsController Approve method for result: {result}");

            return View(model);

        }

        [RoleRequirement(3)] //3 => approver or above only
        private async Task<MDMItemRefAttriApprovalDTO> GetApprovalRecords(string Item, string source)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController GetApprovalRecords method for Item: {Item}");

            var result = await _msmItemRefAttrsService.GetApprovalAsync(Item, source); //TODO
            _logger.LogInformation($"Ending ItemRefAttrsController GetApprovalRecords method for result: {result}");

            return result;
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> ItemAudit()
        {
            _logger.LogInformation($"Starting ItemRefAttrsController ItemAudit method");

            var result = await _msmItemRefAttrsService.ItemAuditGetAsync();
            _logger.LogInformation($"Ending ItemRefAttrsController ItemAudit method for result: {result}");

            return View(result);
        }

        [RoleRequirement(3)] //3 => approver or above only
        private async Task<bool> Update(int auditId, string remarks, bool status)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Update method auditId,remarks,status:{auditId},{remarks},{status}");

            string checkerId = "Ankit";
            var result = await _msmItemRefAttrsService.ApproveOrRejectUpdateAsync(auditId, status, checkerId, remarks);

            _logger.LogInformation($"Ending ItemRefAttrsController Update method result:{result}");
            return result;

        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpGet]
        public async Task<IActionResult> Reject(Int64 id)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Reject method id:{id}");
            var result = await _msmItemRefAttrsService.GetItemAuditAsync(id);
            _logger.LogInformation($"Ending ItemRefAttrsController Reject method result:{result}");

            return View(result);

        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        public async Task<IActionResult> Reject(int auditId, string Remarks)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Reject method auditId,remarks:{auditId},{Remarks}");
            var result = await Update(auditId, Remarks, false);
            if (result)
            {
                _logger.LogInformation($"Ending ItemRefAttrsController Reject method result:{result}");

                TempData["alertMessage"] = "Rejected successfully !";
                return RedirectToAction(nameof(ItemAudit));
            }
            else { return View(); }
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> Approved(int id, string? remarks = null)
        {
            _logger.LogInformation($"Starting ItemRefAttrsController Approved method id:{id}");

            var result = await Update(id, remarks ?? "", true);
            if (result)
            {
                _logger.LogInformation($"Ending ItemRefAttrsController Approved method result:{result}");
                TempData["alertMessage"] = "Item approval successful! The item is now active in the system.";
                return RedirectToAction(nameof(ItemAudit));
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

            PaginatedResult<MDMViewItemRefAttrsDTO> Items = new PaginatedResult<MDMViewItemRefAttrsDTO>();

            if (string.IsNullOrEmpty(search))
            {
                //sortColumn is null, means UpdateDate and SortAcending is null, means descending
                // Items = await _mdmItemRefAttrsService.GetViewItemRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue, null, false);
                Items = await _mdmItemRefAttrsService.GetViewItemRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue, "UpdatedDate",false);
            }
            else
            {
                //Including Global Search
                //sortColumn is null, means UpdateDate and SortAcending is null, means descending
                // Items = await _mdmItemRefAttrsService.GetDataPlusGlobalSearchAsync(pageNumber, pageSize, filterColumnName, filterValue, search, null, false);
                Items = await _mdmItemRefAttrsService.GetDataPlusGlobalSearchAsync(pageNumber, pageSize, filterColumnName, filterValue, search, "UpdatedDate", false);


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
            ViewBag.lstLevel3 = await _mdmLevel3Service.GetDistinctLEVEL3ActiveAsync();
            return View(model);
        }
        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> BulkUpdateSearch(int pageNumber, int pageSize, string filterColumnName, string filterValue, string statusValue, string formula, string selectedPageColumns, string search)
        {
            _logger.LogInformation($"Starting ItemRefAttriController Search method for pageno, pagesize,filterColumnName,filterValue,formula: {pageNumber} ,{pageSize},{filterColumnName},{filterValue},{statusValue},{formula}");

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
        public async Task<ActionResult> BulkUpdate(PaginatedResult<MDMViewItemRefAttrsDTO> model)
        {
            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;


            if (role.LevelNo >= 3)
            {
                if (model.Items != null)
                {
                    var selectedData = model.Items.Where(x => x.IsSelected).ToList();

                    string roleDesc = role.LevelNo == 3 ? "Approver" : "Admin";
                    if (selectedData.Count > 0)
                    {
                        await _msmItemRefAttrsService.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
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
                search = model.search
            });
        }

    }
}
