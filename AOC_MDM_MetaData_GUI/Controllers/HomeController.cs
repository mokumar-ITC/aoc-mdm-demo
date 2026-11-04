using AOC_MDM_MetaData_GUI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Team()
        {
            return View();
        }
        public IActionResult Attribute()    
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Privacy1()
        {
            return View();
        }

        public IActionResult TEST()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
