using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Pinpoint_Quiz.Helpers;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Services;

namespace Pinpoint_Quiz.Controllers
{
    [RoleAuthorize("Teacher", "Administrator", "Developer")]
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
        public IActionResult Index(int? teacherId, bool allSchool = false)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(role) || !userId.HasValue)
                return RedirectToAction("Login", "Account");

            ClassIndexViewModel model = new ClassIndexViewModel();
            if (role == "Teacher")
            {
                // For teacher: get the teacher’s own class.
                var teacher = _userService.GetUserById(userId.Value);
                if (teacher == null)
                    return Forbid();
                model = _classPerf.GetClassPerformance(teacher.ClassId ?? 0, false);
            }
            else // Administrator
            {
                var admin = _userService.GetUserById(userId.Value);
                int schoolId = admin?.SchoolId ?? 0;
                var teachers = _userService.GetTeachersBySchool(schoolId);
                model.TeacherDropdown = teachers
                    .Select(t => new TeacherOption { TeacherId = t.Id, TeacherName = t.FirstName + " " + t.LastName })
                    .ToList();

                if (allSchool)
                {
                    model = _classPerf.GetSchoolPerformance(schoolId);
                    model.ShowWholeSchool = true;
                }
                else if (teacherId.HasValue)
                {
                    var teacher = teachers.FirstOrDefault(t => t.Id == teacherId.Value);
                    if (teacher == null) return Forbid();
                    model = _classPerf.GetClassPerformance(teacher.ClassId ?? 0, true);
                    model.SelectedTeacherId = teacherId;
                }
                else
                {
                    model = _classPerf.GetSchoolPerformance(schoolId);
                    model.ShowWholeSchool = true;
                }
            }

            return View(model);
        }
    }
}
