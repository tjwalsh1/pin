using Microsoft.AspNetCore.Mvc;

namespace Pinpoint_Quiz.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // A simple home page
            return View(); // Views/Home/Index.cshtml
        }

        public IActionResult NoAccess()
        {
            // You can create a view called "NoAccess.cshtml" under Views/Home/
            return View();
        }
    }
}
