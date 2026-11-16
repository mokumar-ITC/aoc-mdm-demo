using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;
using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    // Gradian product master: browse Product Family -> Product Type -> Model -> Size -> Product,
    // and change products through the same maker / checker process as the item master.
    public class ProductMasterController : Controller
    {
        private readonly IProductMasterService _productMaster;
        private readonly IUserRoleService _userRoleService;
        private readonly ILogger<ProductMasterController> _logger;

        public ProductMasterController(IProductMasterService productMaster, IUserRoleService userRoleService,
            ILogger<ProductMasterController> logger)
        {
            _productMaster = productMaster;
            _userRoleService = userRoleService;
            _logger = logger;
        }

        private int UserLevel => _userRoleService.CurrentUserRole?.LevelNo ?? 0;
        private string UserName => _userRoleService.CurrentUserName;
        private string? RoleDesc => _userRoleService.CurrentUserRole?.LevelDesc;

        [RoleRequirement(1)] //1 => read
        public async Task<IActionResult> Index(long? nodeId, string? search, string? status, string? approval)
        {
            ViewBag.UserLevel = UserLevel;
            return View(await _productMaster.BrowseAsync(nodeId, search, status, approval));
        }

        [RoleRequirement(1)]
        public async Task<IActionResult> Details(long id)
        {
            ViewBag.UserLevel = UserLevel;
            var detail = await _productMaster.GetDetailAsync(id);
            return detail == null ? NotFound() : View(detail);
        }

        [RoleRequirement(1)]
        public async Task<IActionResult> Fields()
        {
            return View(await _productMaster.GetFieldsAsync());
        }

        // ------------------------------------------------------------ edit

        [RoleRequirement(2)] //2 => Maker or above
        public async Task<IActionResult> Edit(long id)
        {
            var page = await _productMaster.GetEditAsync(id, UserLevel);
            if (page == null) return NotFound();
            if (page.Product.UpdateStatus == ProductApproval.Pending)
            {
                TempData["errorMessage"] = "This product has a change waiting for approval. It can be edited again once that is approved or rejected.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(2)]
        public async Task<IActionResult> Edit(ProductEditForm form)
        {
            var result = await _productMaster.SubmitEditAsync(form, UserName, UserLevel, RoleDesc);
            if (!result.Success)
            {
                var page = await _productMaster.GetEditAsync(form.ProductId, UserLevel, form);
                if (page == null) return NotFound();
                ViewBag.FormError = result.Message;
                return View(page);
            }

            _logger.LogInformation("{User} (level {Level}) changed product {ProductId}: {Message}", UserName, UserLevel, form.ProductId, result.Message);
            TempData["alertMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = form.ProductId });
        }

        // ------------------------------------------------------------ approval (checker)

        [RoleRequirement(3)] //3 => Checker or above
        public async Task<IActionResult> Approvals()
        {
            return View(await _productMaster.GetPendingAsync());
        }

        [RoleRequirement(3)]
        public async Task<IActionResult> Review(long id)
        {
            var review = await _productMaster.GetReviewAsync(id, UserName);
            return review == null ? NotFound() : View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)]
        public async Task<IActionResult> Approve(long auditId, string? remarks, string? returnTo)
        {
            var result = await _productMaster.ApproveAsync(auditId, UserName, UserLevel, RoleDesc, remarks);
            return AfterDecision(result, auditId, "approved", returnTo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequirement(3)]
        public async Task<IActionResult> Reject(long auditId, string? remarks)
        {
            var result = await _productMaster.RejectAsync(auditId, UserName, UserLevel, RoleDesc, remarks);
            return AfterDecision(result, auditId, "rejected", null);
        }

        private IActionResult AfterDecision(ProductWorkflowResult result, long auditId, string verb, string? returnTo)
        {
            if (!result.Success)
            {
                TempData["errorMessage"] = result.Message;
                return returnTo == "list" ? RedirectToAction(nameof(Approvals)) : RedirectToAction(nameof(Review), new { id = auditId });
            }
            _logger.LogInformation("{User} {Verb} product change request {AuditId}", UserName, verb, auditId);
            TempData["alertMessage"] = result.Message;
            return RedirectToAction(nameof(Approvals));
        }
    }
}
