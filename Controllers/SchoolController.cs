using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Pinpoint_Quiz.Helpers;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Services;

namespace Pinpoint_Quiz.Controllers
{
    [RoleAuthorize("Administrator", "Developer")]
    [Route("School")]
    public class SchoolController : Controller
    {
        private readonly SchoolPerformanceService _schoolPerf;
        private readonly UserService _userService;

        public SchoolController(SchoolPerformanceService schoolPerf, UserService userService)
        {
            _schoolPerf = schoolPerf;
            _userService = userService;
        }

        [HttpGet("")]
        public IActionResult Index(int? teacherId, bool allSchool = false)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(role) || !userId.HasValue)
                return RedirectToAction("Login", "Account");

            // For Admins:
            var admin = _userService.GetUserById(userId.Value);
            int schoolId = admin?.SchoolId ?? 0;
            SchoolIndexViewModel model;
            if (allSchool)
            {
                model = _schoolPerf.GetSchoolPerformance(schoolId);
                model.ShowWholeSchool = true;
            }
            else if (teacherId.HasValue)
            {
                // Here you could filter by a teacher's class if needed:
                model = _schoolPerf.GetSchoolPerformance(schoolId);
                model.SelectedTeacherId = teacherId;
            }
            else
            {
                model = _schoolPerf.GetSchoolPerformance(schoolId);
                model.ShowWholeSchool = true;
            }
            return View(model); // View uses SchoolIndexViewModel
        }

    }
}
