using System.Security.Claims;
using AOC_MDM.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserAccountService _userAccountService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserAccountService userAccountService, IMemoryCache cache,
            IConfiguration configuration, ILogger<AccountController> logger)
        {
            _userAccountService = userAccountService;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return LocalRedirect(SafeReturnUrl(returnUrl));
            await SetDemoAccountsAsync();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string login, string password, string? returnUrl = null)
        {
            var account = await _userAccountService.ValidateAsync(login, password);
            if (account == null)
            {
                _logger.LogWarning("Failed login for {Login}", login);
                ViewBag.Error = "Invalid user name or password, or the account is disabled.";
                ViewBag.Login = login;
                ViewBag.ReturnUrl = returnUrl;
                await SetDemoAccountsAsync();
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, account.Login),
                new("DisplayName", account.DisplayName ?? account.Login),
                new("LevelNo", account.LevelNo.ToString()),
                new("LevelDesc", account.LevelDesc ?? "")
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

            _cache.Remove(account.Login); // pick up the current role on the first request
            _logger.LogInformation("User {Login} logged in (level {Level})", account.Login, account.LevelNo);
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.Name != null) _cache.Remove(User.Identity.Name);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private string SafeReturnUrl(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "SpendArea")!;

        // Demo:ShowDemoAccounts lists the accounts on the login page so a presenter can switch roles quickly
        private async Task SetDemoAccountsAsync()
        {
            if (_configuration.GetValue<bool>("Demo:ShowDemoAccounts"))
            {
                ViewBag.DemoAccounts = (await _userAccountService.GetAllAsync()).Where(a => a.IsActive).OrderBy(a => a.LevelNo).ToList();
                ViewBag.DemoPassword = _configuration["Demo:Password"];
            }
        }
    }
}
