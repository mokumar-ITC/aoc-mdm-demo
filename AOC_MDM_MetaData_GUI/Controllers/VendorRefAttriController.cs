using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM.Services.Service;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Build.Framework;
using System.Drawing.Printing;
using System.Dynamic;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class VendorRefAttriController : Controller
    {
        private readonly IMDMVendorRefAttriService _mdmVendorRefAttriService;
        private readonly IMDMViewVendorRefAttrsService _mdmViewVendorRefAttrsService;
        private readonly IMDMAOCLevel3Service _mdmLevel3Service;
        private readonly ILogger<VendorRefAttriController> _logger;
        // private readonly RoleCacheHelper _roleCacheHelper;
        private readonly IUserRoleService _userRoleService;


        public VendorRefAttriController(IMDMVendorRefAttriService mdmVendorRefAttriService,
            IMDMViewVendorRefAttrsService mdmViewVendorRefAttrsService,
            IMDMAOCLevel3Service mdmLevel3Service,
            ILogger<VendorRefAttriController> logger,
           // RoleCacheHelper roleCacheHelper
           IUserRoleService userRoleService)
        {
            _mdmVendorRefAttriService = mdmVendorRefAttriService;
            _mdmViewVendorRefAttrsService = mdmViewVendorRefAttrsService;
            _mdmLevel3Service = mdmLevel3Service;
            _logger = logger;
            //   _roleCacheHelper = roleCacheHelper;
            _userRoleService = userRoleService;
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Index(int pageNumber, int pageSize, string filterColumnName, string filterValue, string selectedPageColumns)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Index method for pageno, pagesize,filterColumnName,filterValue: {pageNumber} ,{pageSize},{filterColumnName},{filterValue}");
            if (pageNumber == 0)
            {
                pageSize = 25;
                pageNumber = 1;
                filterColumnName = null;
                filterValue = null;

            }
            PaginatedResult<MDMViewVendorRefAttrsDTO> vendors = await
                _mdmViewVendorRefAttrsService.GetViewVendorRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue);
            //Total records
            var totalRecords = vendors.TotalCount;
            vendors.filterValue = filterValue;
            vendors.pageSize = pageSize;
            vendors.pageNumber = pageNumber;
            vendors.filterColumnName = filterColumnName;
            vendors.fromPageNumber = vendors.pageSize * vendors.pageNumber;
            vendors.toPageNumber = vendors.fromPageNumber - vendors.pageSize;
            vendors.totalPages = (totalRecords / pageSize) + 1;
            vendors.selectedPageColumns = selectedPageColumns;
            dynamic model = new ExpandoObject();
            model = vendors;
            _logger.LogInformation($"End VendorRefAttriController Index method list,totalRecords: {vendors.Items.Count()} ,{totalRecords}");
            return View(model);
        }

        [RoleRequirement(3)] //3 => approver or above only
        private async Task<MDMVendorRefAttriApprovalDTO> GetApprovalRecords(string Id, string source)
        {
            _logger.LogInformation($"Starting VendorRefAttriController GetApprovalRecords method for Id: {Id}");


            var result = await _mdmVendorRefAttriService.GetApprovalAsync(Id, source);//TODO
            _logger.LogInformation($"Ending VendorRefAttriController GetApprovalRecords method for result: {result}");
            return result;
        }

        [RoleRequirement(3)] //3 => approver or above only
        //Checker Process for insert
        private async Task<bool> Update(int auditId, string remarks, bool status)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Update method for auditId,remarks,status: {auditId},{remarks},{status}");

            var username = _userRoleService.CurrentUserName;
            var role = _userRoleService.CurrentUserRole;

            string checkerId = username;
            var result = await _mdmVendorRefAttriService.ApproveOrRejectUpdateAsync(auditId, status, checkerId, remarks);
            _logger.LogInformation($"Ending VendorRefAttriController Update method for result: {result}");
            return result;
        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpGet]
        public async Task<IActionResult> Reject(Int64 id)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Reject method for Id: {id}");
            var vendorAudit = await _mdmVendorRefAttriService.VendorAuditGetAsync(id);
            _logger.LogInformation($"Ending VendorRefAttriController Reject method for result count: {vendorAudit}");
            return View(vendorAudit);
        }

        [RoleRequirement(3)] //3 => approver or above only
        [HttpPost]
        public async Task<IActionResult> Reject(int auditId, string Remarks)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Reject method for auditId,remarks: {auditId},{Remarks}");
            var result = await Update(auditId, Remarks, false);
            if (result)
            {
                _logger.LogInformation($"Ending VendorRefAttriController Reject method for result: {result}");
                TempData["alertMessage"] = "Rejected successfully !";
                return RedirectToAction(nameof(VendorAudit));
            }
            else { return View(); }
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> Approved(int id, string? remarks = null)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Approved method for Id: {id}");
            var result = await Update(id, remarks ?? "", true);
            if (result)
            {
                _logger.LogInformation($"Ending VendorRefAttriController Approved method for result: {result}");
                TempData["alertMessage"] = "Vendor approval successful! The vendor is now active in the system.";
                return RedirectToAction(nameof(VendorAudit));
            }
            else { return View(); }
        }

        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> VendorAudit()
        {
            _logger.LogInformation($"Starting VendorRefAttriController VendorAudit method");

            var result = await _mdmVendorRefAttriService.VendorAuditGetAsync();
            _logger.LogInformation($"Ending VendorRefAttriController VendorAudit method for result count: {result.Count()}");

            return View(result);
        }

        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> Search(int pageNumber, int pageSize, string filterColumnName, string filterValue, string statusValue, string formula, string selectedPageColumns)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Search method for pageno, pagesize,filterColumnName,filterValue: {pageNumber} ,{pageSize},{filterColumnName},{filterValue},{statusValue}");

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
        [HttpPost]
        public async Task<IActionResult> pagination(int pageNumber)
        {
            _logger.LogInformation($"Starting VendorRefAttriController pagination method for pageno: {pageNumber}");

            PaginatedResult<MDMViewVendorRefAttrsDTO> vendors = await
              _mdmViewVendorRefAttrsService.GetViewVendorRefAttrsAsync(pageNumber, 10, null, null);
            var totalRecords = vendors.TotalCount;
            return Json(vendors.Items);
            //return View();
        }

        [RoleRequirement(2)] //2 => Maker role (min role) 
        // GET: VendorRefAttriController/Edit/5
        public async Task<IActionResult> Edit(string id, string VendorName, string VendorNumber, string Source,string SpendArea)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Edit method for Id,Source: {id},{Source}");

            var result = await _mdmVendorRefAttriService.GetFilteredAsync(id, Source);
            ViewBag.lstLevel3 = await _mdmLevel3Service.GetDistinctLEVEL3ActiveAsync();
            ViewBag.VendorFamilyName = await _mdmVendorRefAttriService.GetDistinctColumnValuesAsync("vendor_family_name");
            ViewBag.VendorEnglishName = await _mdmVendorRefAttriService.GetDistinctColumnValuesAsync("vendor_english_name");
            ViewBag.IndirectSpendOwner = await _mdmVendorRefAttriService.GetDistinctColumnValuesAsync("indirect_spend_owner");
            ViewBag.ParentCompany = await _mdmVendorRefAttriService.GetDistinctColumnValuesAsync("parent_company");
            ViewBag.SupplierVendor = await _mdmVendorRefAttriService.GetDistinctColumnValuesAsync("supplier_vendor");
            //var result = products.FirstOrDefault();
            MDMViewVendorRefAttrsDTO mDMVendorRefAttriDTO = new MDMViewVendorRefAttrsDTO();
            mDMVendorRefAttriDTO.VendorId = result.VendorId;
            mDMVendorRefAttriDTO.SupplierVendor = result.SupplierVendor;
            mDMVendorRefAttriDTO.AddressableSpend = result.AddressableSpend;
            mDMVendorRefAttriDTO.Intercompany = result.Intercompany;
            mDMVendorRefAttriDTO.IndirectSpendOwner = result.IndirectSpendOwner;
            mDMVendorRefAttriDTO.Level3 = result.Level3;
            mDMVendorRefAttriDTO.ParentCompany = result.ParentCompany;
            mDMVendorRefAttriDTO.PayablesVeryImportantSupplier = result.PayablesVeryImportantSupplier;
            mDMVendorRefAttriDTO.VendorEnglishName = result.VendorEnglishName;
            mDMVendorRefAttriDTO.VendorFamilyName = result.VendorFamilyName;
            mDMVendorRefAttriDTO.Source = result.Source;
            mDMVendorRefAttriDTO.VendorNumber = VendorNumber;
            mDMVendorRefAttriDTO.VendorName = VendorName;
            mDMVendorRefAttriDTO.SpendArea = SpendArea;
            return View(mDMVendorRefAttriDTO);
        }

        // GET: VendorRefAttriController/Approve/5
        [RoleRequirement(3)] //3 => approver or above only
        public async Task<IActionResult> Approve(string id, string source)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Approve method for Id: {id}");

            var result = await GetApprovalRecords(id, source);
            dynamic model = new ExpandoObject();
            if (result != null)
            {
                model.MainTable = result.MainTable;
                model.AuditTable = result.AuditTable;
            }
            _logger.LogInformation($"End VendorRefAttriController Approve method for result: {result}");

            return View(model);
        }

        // POST: VendorRefAttriController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(2)] //2 => maker role (min.)
        public async Task<IActionResult> Edit(string id, [FromForm] MDMVendorRefAttriDTO collection)
        {
            try
            {
                _logger.LogInformation($"Starting VendorRefAttriController Edit method for Id,collection: {id},{collection}");
                bool status = false;

                //bool roleFound;
                //var userRole = _roleCacheHelper.GetCachedUserRole(HttpContext, out roleFound);

                var username = _userRoleService.CurrentUserName;
                var role = _userRoleService.CurrentUserRole;

                MDMVendorRefAttriDTO mDMVendorRefAttriDTO = new MDMVendorRefAttriDTO();
                mDMVendorRefAttriDTO.VendorId = collection.VendorId;
                mDMVendorRefAttriDTO.SupplierVendor = collection.SupplierVendor;
                mDMVendorRefAttriDTO.AddressableSpend = collection.AddressableSpend;
                mDMVendorRefAttriDTO.Intercompany = collection.Intercompany;
                mDMVendorRefAttriDTO.IndirectSpendOwner = collection.IndirectSpendOwner;
                mDMVendorRefAttriDTO.Level3 = collection.Level3;
                mDMVendorRefAttriDTO.ParentCompany = collection.ParentCompany;
                mDMVendorRefAttriDTO.PayablesVeryImportantSupplier = collection.PayablesVeryImportantSupplier;
                mDMVendorRefAttriDTO.VendorEnglishName = collection.VendorEnglishName;
                mDMVendorRefAttriDTO.VendorFamilyName = collection.VendorFamilyName;
                mDMVendorRefAttriDTO.Source = collection.Source;
                string makerId = username;// "Ankit";

                if (role.LevelNo >= 3)
                {
                    _logger.LogInformation($"User role level is {role?.LevelNo}. Calling updateDirectly for ID: {id}");
                    status = await _mdmVendorRefAttriService.UpdateDirectlyAsync(collection.VendorId, mDMVendorRefAttriDTO, true, makerId, role.LevelDesc, "");
                }
                else
                {

                    status = await _mdmVendorRefAttriService.UpdateReqByMakerAsync(collection.VendorId, mDMVendorRefAttriDTO, makerId, "");
                    // var status = await _mdmVendorRefAttriService.UpdateDirectlyAsync(collection.VendorId, mDMVendorRefAttriDTO, true ,makerId, role.LevelDesc, "");
                }

                if (status)
                {
                    _logger.LogInformation($"End VendorRefAttriController Edit method for result: {status}");

                    TempData["alertMessage"] = role.LevelNo >= 3 ? "Record updated successfully" : "Your vendor request is now pending approval. Please check with the approver (Checker) for updates.";
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

        public async Task<ActionResult> BulkUpdate(int pageNumber, int pageSize, string filterColumnName, string filterValue, 
            string selectedPageColumns,string search)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Index method for pageno, pagesize,filterColumnName,filterValue,search: {pageNumber} ,{pageSize},{filterColumnName},{filterValue}");
            if (pageNumber == 0)
            {
                pageSize = 25;
                pageNumber = 1;
                filterColumnName = null;
                filterValue = null;
            }
            PaginatedResult<MDMViewVendorRefAttrsDTO> vendors = new PaginatedResult<MDMViewVendorRefAttrsDTO>();
            
            if (string.IsNullOrEmpty(search))
            {
                vendors = await _mdmViewVendorRefAttrsService.GetViewVendorRefAttrsAsync(pageNumber, pageSize, filterColumnName, filterValue);
            }
            else
            {
                //Including Global Search
                vendors = await _mdmViewVendorRefAttrsService.GetDataPlusGlobalSearchAsync(pageNumber, pageSize, filterColumnName, filterValue,search);

            }
            //Total records
            var totalRecords = vendors.TotalCount;
            vendors.filterValue = filterValue;
            vendors.pageSize = pageSize;
            vendors.pageNumber = pageNumber;
            vendors.filterColumnName = filterColumnName;
            vendors.fromPageNumber = vendors.pageSize * vendors.pageNumber;
            vendors.toPageNumber = vendors.fromPageNumber - vendors.pageSize;
            vendors.totalPages = (totalRecords / pageSize) + 1;
            vendors.selectedPageColumns = selectedPageColumns;
            vendors.search = search;
            dynamic model = new ExpandoObject();
            model = vendors;
            ViewBag.lstLevel3 = await _mdmLevel3Service.GetDistinctLEVEL3ActiveAsync();
            return View(model);
        }
        [RoleRequirement(1)] //1 => read (minimum role)
        public async Task<IActionResult> BulkUpdateSearch(int pageNumber, int pageSize, string filterColumnName, string filterValue, string statusValue, string formula, string selectedPageColumns,string search)
        {
            _logger.LogInformation($"Starting VendorRefAttriController Search method for pageno, pagesize,filterColumnName,filterValue,formula,search: {pageNumber} ,{pageSize},{filterColumnName},{filterValue},{statusValue},{formula}");

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
        public async Task<ActionResult> BulkUpdate(PaginatedResult<MDMViewVendorRefAttrsDTO> model)
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
                        await _mdmVendorRefAttriService.BulkUpdateAsync(selectedData, username, roleDesc, "Bulk Update!");
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
