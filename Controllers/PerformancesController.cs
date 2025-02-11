using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Services;
using System.Linq;
using Pinpoint_Quiz.Dtos;

namespace Pinpoint_Quiz.Controllers
{
    [Route("Performances")]
    public class PerformancesController : Controller
    {
        private readonly QuizService _quizService;

        public PerformancesController(QuizService quizService)
        {
            _quizService = quizService;
        }

        // This action accepts an optional query parameter "userId"
        [HttpGet("Progress")]
        public IActionResult Progress(int? userId)
        {
            // Get the signed-in user's id from session
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string role = HttpContext.Session.GetString("UserRole") ?? "Student";

            // Allow teachers/administrators to view progress for a different user if "userId" is supplied.
            int effectiveUserId = (role == "Teacher" || role == "Administrator") && userId.HasValue
                ? userId.Value
                : currentUserId;

            // Retrieve quiz history for effectiveUserId (your actual implementation may vary)
            var history = _quizService.GetLast10Quizzes(effectiveUserId)
                           .OrderBy(q => q.QuizDate)
                           .ToList();

            var model = new PerformanceChartViewModel
            {
                LabelDates = history.Select(q => q.QuizDate.ToLocalTime().ToString("yyyy-MM-dd")).ToList(),
                MathLevels = history.Select(q => q.MathProficiency).ToList(),
                EbrwLevels = history.Select(q => q.EbrwProficiency).ToList(),
                // (Set your actual proficiency arrays if available)
                ActualMathLevels = history.Select(q => q.ActualMathProficiency).ToList(),
                ActualEbrwLevels = history.Select(q => q.ActualEbrwProficiency).ToList(),
                ActualOverallLevels = history.Select(q => q.ActualOverallProficiency).ToList(),
                TimeElapsed = history.Select(q => q.TimeElapsed).ToList(),
                QuizHistory = history.Select(q => new QuizHistoryDto
                {
                    QuizDate = q.QuizDate,
                    MathProficiency = q.MathProficiency,
                    EbrwProficiency = q.EbrwProficiency,
                    OverallProficiency = q.OverallProficiency,
                    MathCorrect = q.MathCorrect,
                    EbrwCorrect = q.EbrwCorrect,
                    MathTotal = q.MathTotal,
                    EbrwTotal = q.EbrwTotal,
                    QuizId = q.Id,
                    ActualMathProficiency = q.ActualMathProficiency,
                    ActualEbrwProficiency = q.ActualEbrwProficiency,
                    ActualOverallProficiency = q.ActualOverallProficiency
                }).ToList()
            };

            return View(model);
        }
    }
}
