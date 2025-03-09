using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Pinpoint_Quiz.Helpers;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Services;

namespace Pinpoint_Quiz.Controllers
{
    [RoleAuthorize("Teacher", "Administrator")]
    [Route("[controller]")]
    public class ClassController : Controller
    {
        private readonly ClassPerformanceService _classPerf;
        private readonly UserService _userService;

        public ClassController(ClassPerformanceService classPerf, UserService userService)
        {
            _classPerf = classPerf;
            _userService = userService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(role) || !userId.HasValue)
                return RedirectToAction("Login", "Account");

/*
            if (role == "Administrator")
            {
                return RedirectToAction("Index", "School");
            } */

            // Otherwise, assume teacher role
            var teacher = _userService.GetUserById(userId.Value);
            if (teacher == null)
                return Forbid();
            // Get teacher’s own class performance.
            ClassIndexViewModel model = _classPerf.GetClassPerformance(teacher.ClassId ?? 0);
            return View(model); // The view is strongly typed to ClassIndexViewModel
        }
    }
}
