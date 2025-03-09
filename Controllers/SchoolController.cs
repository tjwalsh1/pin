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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            // Get the admin's record to retrieve the schoolId.
            var admin = _userService.GetUserById(userId.Value);
            if (admin == null || admin.SchoolId == null)
                return Forbid();

            int schoolId = admin.SchoolId.Value;

            SchoolIndexViewModel model = new SchoolIndexViewModel();

            // Populate teacher dropdown using the service.
            model.Teachers = _schoolPerf.GetTeacherRowsForSchool(schoolId);

            if (allSchool)
            {
                model = _schoolPerf.GetSchoolPerformance(schoolId);
            }
            else if (teacherId.HasValue)
            {
                // Optionally: you might filter to a specific teacher's class.
                // For now, we simply note the teacher selection.
                model = _schoolPerf.GetSchoolPerformance(schoolId);
                model.SelectedTeacherId = teacherId;
            }
            else
            {
                // Default to whole school view.
                model = _schoolPerf.GetSchoolPerformance(schoolId);
            }

            return View(model);
        }
    }
}
