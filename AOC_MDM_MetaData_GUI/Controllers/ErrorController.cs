using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AOC_MDM_MetaData_GUI.Controllers
{
    public class ErrorController : Controller
    {

        [Route("Error/AccessDenied")]
        public IActionResult AccessDenied(int? required)
        {
            var levels = new Dictionary<int, string> { [1] = "Viewer", [2] = "Maker", [3] = "Checker", [4] = "Admin" };
            var message = "You do not have the necessary permissions to access this resource.";
            if (required != null && levels.TryGetValue(required.Value, out var name))
                message = $"This page requires {name} access (level {required}) or higher.";
            return View((object)message);
        }

        [Route("Error/Handle")]
        public IActionResult Handle()
        {
            var exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

            if (exception is UnauthorizedAccessException)
            {
                // Return a custom view for unauthorized access
                return View("AccessDenied", exception.Message);
            }

            // Generic error handler for all other exceptions
            ViewBag.Message = "An unexpected error occurred. Please try again later.";
            return View("Error");
        }
    }

}
