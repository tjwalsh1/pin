using Microsoft.AspNetCore.Mvc;
using Pinpoint_Quiz.Services;
using Pinpoint_Quiz.Dtos;
using System.Collections.Generic;

namespace Pinpoint_Quiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesApiController : ControllerBase
    {
        private readonly QuizService _quizService;

        public QuizzesApiController(QuizService quizService)
        {
            _quizService = quizService;
        }

        // POST: /api/QuizzesApi/generate?studentId=123&adaptive=true
        [HttpPost("generate")]
        public IActionResult GenerateQuiz([FromQuery] int studentId, [FromQuery] bool adaptive = false)
        {
            List<QuestionDto> mathQuestions;
            List<QuestionDto> ebrwQuestions;

            if (adaptive)
            {
                mathQuestions = _quizService.GenerateAdaptiveQuiz(studentId, "Math", 10);
                ebrwQuestions = _quizService.GenerateAdaptiveQuiz(studentId, "EBRW", 10);
            }
            else
            {
                mathQuestions = _quizService.GenerateNonAdaptiveQuiz(studentId, "Math", 10);
                ebrwQuestions = _quizService.GenerateNonAdaptiveQuiz(studentId, "EBRW", 10);
            }

            return Ok(new
            {
                StudentId = studentId,
                MathQuestions = mathQuestions,
                EbrwQuestions = ebrwQuestions
            });
        }

        // POST: /api/QuizzesApi/submit?studentId=123&quizId=1
        [HttpPost("submit")]
        public IActionResult SubmitQuiz([FromQuery] int studentId, [FromQuery] int quizId, [FromBody] QuizSubmissionDto submission)
        {
            bool success = _quizService.SubmitQuiz(studentId, quizId, submission);
            if (!success)
            {
                return StatusCode(500, "Failed to submit quiz");
            }
            return Ok("Quiz submitted successfully");
        }
    }
}
