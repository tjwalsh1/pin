using Microsoft.AspNetCore.Mvc;
using Pinpoint_Quiz.Services;
using Pinpoint_Quiz.Models;
using System.Collections.Generic;
using Pinpoint_Quiz.Models.Pinpoint_Quiz.Models;

namespace Pinpoint_Quiz.Controllers
{
    [Route("[controller]")]
    public class LessonsController : Controller
    {
        private readonly LessonService _lessonService;

        public LessonsController(LessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet("")]
        public IActionResult Index(string subject = null)
        {
            List<Lesson> lessons = _lessonService.GetAllLessons(subject);
            return View(lessons);
        }

        [HttpGet("details/{id}")]
        public IActionResult Details(int id)
        {
            Lesson lesson = _lessonService.GetLesson(id);
            if (lesson == null)
            {
                return NotFound();
            }
            return View(lesson);
        }
    }
}
