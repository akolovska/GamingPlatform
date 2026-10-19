using System.Diagnostics;
using GamingPlatform.Domain;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    public class HomeController : Controller
    {
        // The store is the home page
        public IActionResult Index() => RedirectToAction("Index", "Games");

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        // Shown for 404 and other status codes (see UseStatusCodePagesWithReExecute in Program.cs)
        [Route("Home/Status/{code:int}")]
        public IActionResult Status(int code) => View(code);
    }
}
