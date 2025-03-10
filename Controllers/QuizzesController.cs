﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Services;
using System;
using System.Linq;
using Pinpoint_Quiz.Helpers;
using Pinpoint_Quiz.Dtos;

namespace Pinpoint_Quiz.Controllers
{
    [Route("[controller]")]
    public class QuizzesController : Controller
    {
        private readonly QuizService _quizService;
        private readonly AccoladeService _accoladeService;
        private readonly ILogger<QuizzesController> _logger;
        private readonly AIQuizService _aiQuizService;

        public QuizzesController(QuizService quizService, AccoladeService accoladeService, ILogger<QuizzesController> logger, AIQuizService aiQuizService)
        {
            _quizService = quizService;
            _accoladeService = accoladeService;
            _logger = logger;
            _aiQuizService = aiQuizService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            double startingDifficulty = 0;
            if (userId.HasValue)
            {
                double math = _quizService.GetUserProficiency(userId.Value, "Math");
                double ebrw = _quizService.GetUserProficiency(userId.Value, "EBRW");
                startingDifficulty = (math + ebrw) / 2.0;
            }
            var model = new QuizzesIndexViewModel
            {
                StartingDifficulty = startingDifficulty,
                IsLoggedIn = userId.HasValue
            };
            return View(model);
        }
        [HttpGet("start")]
        public IActionResult StartQuiz(string mode = "Normal")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var difficultyMode = Enum.TryParse<DifficultyMode>(mode, out var parsedMode)
                ? parsedMode
                : DifficultyMode.Normal;

            var quizSession = new QuizSession
            {
                UserId = userId.Value,
                IsAdaptive = true, // or false, as you wish
                LocalMath = _quizService.GetUserProficiency(userId.Value, "Math"),
                LocalEbrw = _quizService.GetUserProficiency(userId.Value, "EBRW"),
                TimeStarted = DateTime.Now,
                DifficultyMode = difficultyMode
            };

            HttpContext.Session.SetObject("QuizSession", quizSession);
            return RedirectToAction("NextQuestion");
        }


        // Start an adaptive quiz
        [HttpGet("start-adaptive")]
        public IActionResult StartAdaptive()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var quizSession = new QuizSession
            {
                UserId = userId.Value,
                IsAdaptive = true,
                LocalMath = _quizService.GetUserProficiency(userId.Value, "Math"),
                LocalEbrw = _quizService.GetUserProficiency(userId.Value, "EBRW"),
                TimeStarted = DateTime.Now  // Record the start time here

            };

            HttpContext.Session.SetObject("QuizSession", quizSession);
            return RedirectToAction("NextQuestion");
        }

        int GetTargetDifficulty(double proficiency, DifficultyMode mode)
        {
            // clamp proficiency to [1..10]
            proficiency = Math.Min(10, Math.Max(1, proficiency));

            switch (mode)
            {
                case DifficultyMode.Easy:
                    // up to 2 below proficiency, floor at 1
                    var easyVal = proficiency - 2;
                    return (int)Math.Max(1, Math.Round(easyVal));
                case DifficultyMode.Hard:
                    // up to 2 above proficiency, cap at 10
                    var hardVal = proficiency + 2;
                    return (int)Math.Min(10, Math.Round(hardVal));
                default:
                    // Normal
                    return (int)Math.Round(proficiency);
            }
        }


        // Serve the next question
        [HttpGet("NextQuestion")]
        public IActionResult NextQuestion()
        {

            var quizSession = HttpContext.Session.GetObject<QuizSession>("QuizSession");
            if (quizSession == null) return RedirectToAction("Index");

            // If we've asked enough questions, go to Submit
            if (quizSession.CurrentIndex >= quizSession.TotalQuestions)
                return RedirectToAction("SubmitQuiz");

            bool doEbrw = (quizSession.EbrwCount < 5);
            int targetDifficulty;

            var question = doEbrw
                ? _quizService.GetQuestionByDifficulty("EBRW", (int)Math.Round(quizSession.LocalEbrw))
                : _quizService.GetQuestionByDifficulty("Math", (int)Math.Round(quizSession.LocalMath));

            if (doEbrw)
            {
                targetDifficulty = GetTargetDifficulty(quizSession.LocalEbrw, quizSession.DifficultyMode);
                question = _quizService.GetQuestionByDifficulty("EBRW", targetDifficulty);
            }
            else
            {
                targetDifficulty = GetTargetDifficulty(quizSession.LocalMath, quizSession.DifficultyMode);
                question = _quizService.GetQuestionByDifficulty("Math", targetDifficulty);
            }

            // Add to session record
            quizSession.Questions.Add(new QuestionRecord
            {
                Subject = question.Subject,
                Dto = question,
                UserCorrect = null
            });

            if (doEbrw) quizSession.EbrwCount++;
            else quizSession.MathCount++;

            quizSession.CurrentIndex++;
            HttpContext.Session.SetObject("QuizSession", quizSession);

            return View("SingleQuestion", new SingleQuestionViewModel
            {
                StudentId = quizSession.UserId,
                QuestionNumber = quizSession.CurrentIndex,
                TotalQuestions = quizSession.TotalQuestions,
                Prompt = question.QuestionPrompt,
                Answers = question.ShuffledAnswers,
                CorrectAnswer = question.CorrectAnswer,
                Explanation = question.Explanation,
                Difficulty = question.Difficulty,
                Subject = question.Subject,
                QuestionId = question.Id  // Make sure your QuestionDto has an Id property.
            });

        }

        // Handle answer submission from SingleQuestion
        [HttpPost("Answer")]
        public IActionResult Answer(int questionNumber, string selectedAnswer, string correctAnswer, string subject)
        {
            var quizSession = HttpContext.Session.GetObject<QuizSession>("QuizSession");
            if (quizSession == null) return RedirectToAction("Index");

            bool isCorrect = selectedAnswer == correctAnswer;

            // Update the current question record
            quizSession.Questions[questionNumber - 1].UserCorrect = isCorrect;
            quizSession.Questions[questionNumber - 1].Dto.YourAnswer = selectedAnswer;

            // Optionally, save the response in a log
            _quizService.SaveQuestionResponse(
                quizSession.UserId,
                quizSession.QuizId,
                quizSession.Questions[questionNumber - 1].Dto.QuestionPrompt,
                selectedAnswer,
                isCorrect
            );

            // Retrieve current actual proficiencies
            var currentProfs = _quizService.GetActualProficiencies(quizSession.UserId);
            double currentMath = currentProfs.math;
            double currentEbrw = currentProfs.ebrw;

            // Calculate new proficiency for the subject answered
            if (subject == "Math")
            {
                double newMath = _quizService.ApplyProficiencyChange(
                    currentMath,
                    quizSession.Questions[questionNumber - 1].Dto.Difficulty,
                    isCorrect,
                    quizSession.RetakeMode
                );
                // Update Math while keeping EBRW unchanged
                _quizService.UpdateUserProficiency(quizSession.UserId, newMath, currentEbrw);
            }
            else if (subject == "EBRW")
            {
                double newEbrw = _quizService.ApplyProficiencyChange(
                    currentEbrw,
                    quizSession.Questions[questionNumber - 1].Dto.Difficulty,
                    isCorrect,
                    quizSession.RetakeMode
                );
                _quizService.UpdateUserProficiency(quizSession.UserId, currentMath, newEbrw);
            }

            // Optionally, re-read and log updated proficiency values here

            // Update quiz session counts and index
            if (subject == "Math")
                quizSession.MathCount++;
            else if (subject == "EBRW")
                quizSession.EbrwCount++;

            quizSession.CurrentIndex++;
            HttpContext.Session.SetObject("QuizSession", quizSession);

            return RedirectToAction("NextQuestion");
        }


        [HttpGet("start-retake")]
        public IActionResult StartRetake()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            // Retrieve the most recent quiz result for this user.
            var latestResult = _quizService.GetLatestQuizResult(userId.Value);
            if (latestResult == null || latestResult.QuestionResults == null || latestResult.QuestionResults.Count == 0)
            {
                TempData["ErrorMessage"] = "No previous quiz found to retake.";
                return RedirectToAction("Index");
            }

            // Create a new QuizSession using the same question set from the most recent quiz.
            var newSession = new QuizSession
            {
                UserId = userId.Value,
                IsAdaptive = false, // or copy from the original if needed
                                    // Use the proficiency values from the latest quiz result (or set defaults)
                LocalMath = latestResult.FinalProficiencyMath,
                LocalEbrw = latestResult.FinalProficiencyEbrw,
                QuizId = 0, // new attempt; this will generate a new QuizResults row on submission
                RetakeMode = true // Mark this as a retake if you want to check it later (for logging, UI, etc.)
            };

            // For each question from the latest quiz result, add a new QuestionRecord to the session.
            // (This copies the question details but does not copy over any answers.)
            foreach (var q in latestResult.QuestionResults)
            {
                var qDto = new QuestionDto
                {
                    // Copy over the question details
                    QuestionPrompt = q.QuestionPrompt,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation,
                    Difficulty = q.Difficulty,
                    Subject = q.Subject,
                    // Do NOT copy qDto.YourAnswer; let it remain null.
                    // Also copy wrong answers if applicable.
                };

                newSession.Questions.Add(new QuestionRecord
                {
                    Subject = qDto.Subject,
                    Dto = qDto,
                    UserCorrect = null
                });
            }

            // Log for debugging:
            _logger.LogInformation("StartRetake: New session created with {Count} questions; RetakeMode set to {RetakeMode}.", newSession.Questions.Count, newSession.RetakeMode);

            // Save the new session in the user session.
            HttpContext.Session.SetObject("QuizSession", newSession);

            // Redirect to the first question of the new quiz.
            return RedirectToAction("NextQuestion");
        }

        [HttpGet("SubmitQuiz")]
        public IActionResult SubmitQuiz()
        {
            var quizSession = HttpContext.Session.GetObject<QuizSession>("QuizSession");
            if (quizSession == null)
                return RedirectToAction("Index");

            DateTime timeStarted = quizSession.TimeStarted;
            DateTime timeEnded = DateTime.Now;
            double timeElapsed = (timeEnded - timeStarted).TotalSeconds;

            // Build the question results list.
            var questionResults = quizSession.Questions
                .Where(q => q.Dto != null)
                .Select(q => new QuestionResultDto
                {
                    QuestionPrompt = q.Dto.QuestionPrompt,
                    YourAnswer = q.Dto.YourAnswer,
                    CorrectAnswer = q.Dto.CorrectAnswer,
                    Explanation = q.Dto.Explanation,
                    IsCorrect = q.UserCorrect ?? false,
                    Subject = q.Subject,
                    Difficulty = q.Dto.Difficulty
                }).ToList();

            if (!quizSession.IsAdaptive)
            {
                // For a non-adaptive (AI) quiz, we expect 5 Math and 5 EBRW questions.
                int totalMath = 5;   // should be 5
                int totalEbrw = 5;     // should be 5
                int correctMath = quizSession.Questions.Count(q => q.Subject == "Math" && q.UserCorrect == true);
                int correctEbrw = quizSession.Questions.Count(q => q.Subject == "EBRW" && q.UserCorrect == true);

                // Calculate displayed scores on a 5-point scale per subject.
                double mathScoreForDisplay = totalMath > 0 ? ((double)correctMath / totalMath) * 5 : 0;
                double ebrwScoreForDisplay = totalEbrw > 0 ? ((double)correctEbrw / totalEbrw) * 5 : 0;
                double overallScoreForDisplay = mathScoreForDisplay + ebrwScoreForDisplay; // overall out of 10

                _logger.LogInformation("Non-adaptive quiz display: Math: {MathScore}, EBRW: {EbrwScore}, Overall: {OverallScore}",
                    mathScoreForDisplay, ebrwScoreForDisplay, overallScoreForDisplay);

                // Instead of updating proficiency from the aggregated score,
                // update actual proficiency using the same incremental logic as in your adaptive quiz.
                _quizService.UpdateActualProficiency(quizSession.UserId, questionResults, quizSession.RetakeMode);

                // Retrieve updated proficiency values.
                var (actualMath, actualEbrw, actualOverall) = _quizService.GetActualProficiencies(quizSession.UserId);

                // Save quiz results using the display scores.
                int quizId = _quizService.SaveQuizResults(
                    quizSession.UserId,
                    mathScoreForDisplay,    // math score out of 5
                    ebrwScoreForDisplay,    // EBRW score out of 5
                    overallScoreForDisplay, // overall score out of 10
                    correctMath,
                    correctEbrw,
                    totalMath,
                    totalEbrw,
                    questionResults,
                    actualMath, actualEbrw, actualOverall,
                    timeStarted, timeEnded, timeElapsed
                );

                var newlyAwarded = _accoladeService.CheckAndAwardAccolades(
                    quizSession.UserId,
                    quizSession.RetakeMode,
                    correctMath + correctEbrw, // total correct answers
                    totalMath + totalEbrw      // total questions (should be 10)
                );

                if (newlyAwarded.Any())
                {
                    ViewBag.AccoladeMessage = "Congrats! You earned new accolade(s): " + string.Join(", ", newlyAwarded);
                }
                TempData["AccoladeMessage"] = "Congrats! You earned a new accolade!";

                HttpContext.Session.Remove("QuizSession");

                return RedirectToAction("Results", new { quizId });
            }
            else
            {
                // Adaptive branch remains unchanged.
                int correctMath = quizSession.Questions.Count(q => q.Subject == "Math" && q.UserCorrect == true);
                int correctEbrw = quizSession.Questions.Count(q => q.Subject == "EBRW" && q.UserCorrect == true);
                int totalMath = quizSession.Questions.Count(q => q.Subject == "Math");
                int totalEbrw = quizSession.Questions.Count(q => q.Subject == "EBRW");

                double overallScore = (quizSession.LocalMath + quizSession.LocalEbrw) / 2.0;

                _quizService.UpdateActualProficiency(quizSession.UserId, questionResults, quizSession.RetakeMode);
                var (actualMath, actualEbrw, actualOverall) = _quizService.GetActualProficiencies(quizSession.UserId);

                int quizId = _quizService.SaveQuizResults(
                    quizSession.UserId,
                    quizSession.LocalMath,
                    quizSession.LocalEbrw,
                    overallScore,
                    correctMath,
                    correctEbrw,
                    totalMath,
                    totalEbrw,
                    questionResults,
                    actualMath, actualEbrw, actualOverall,
                    timeStarted, timeEnded, timeElapsed
                );

                var newlyAwarded = _accoladeService.CheckAndAwardAccolades(
                    quizSession.UserId,
                    quizSession.RetakeMode,
                    correctMath + correctEbrw,
                    totalMath + totalEbrw
                );

                if (newlyAwarded.Any())
                {
                    ViewBag.AccoladeMessage = "Congrats! You earned new accolade(s): " + string.Join(", ", newlyAwarded);
                }
                TempData["AccoladeMessage"] = "Congrats! You earned a new accolade!";

                HttpContext.Session.Remove("QuizSession");

                return RedirectToAction("Results", new { quizId });
            }
        }

        // Show quiz results
        [HttpGet("Results")]
        public IActionResult Results(int quizId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var results = _quizService.GetQuizResults(userId.Value, quizId);

            // If no results were found or the question details list is empty, show a fallback view.
            if (results == null || results.QuestionResults == null || results.QuestionResults.Count == 0)
            {
                return View("NoResults");
            }

            // Otherwise, assign the StudentId (if needed)
            results.StudentId = userId.Value;
            return View("QuizResults", results);
        }
        [HttpGet("DisplayQuestion")]
        public IActionResult DisplayQuestion(int questionNumber)
        {
            var quizSession = HttpContext.Session.GetObject<QuizSession>("QuizSession");
            if (quizSession == null)
                return RedirectToAction("Index");

            // Validate question number
            if (questionNumber < 1 || questionNumber > quizSession.Questions.Count)
                return RedirectToAction("SubmitQuiz"); // Or handle error appropriately

            // Retrieve the question record from the session
            var questionRecord = quizSession.Questions[questionNumber - 1];

            // Build the view model using the data from the questionRecord's Dto
            var viewModel = new SingleQuestionViewModel
            {
                StudentId = quizSession.UserId,
                QuestionNumber = questionNumber, // use the passed question number
                TotalQuestions = quizSession.TotalQuestions,
                Prompt = questionRecord.Dto.QuestionPrompt,
                Answers = questionRecord.Dto.ShuffledAnswers,
                CorrectAnswer = questionRecord.Dto.CorrectAnswer,
                Explanation = questionRecord.Dto.Explanation,
                Difficulty = questionRecord.Dto.Difficulty,
                Subject = questionRecord.Subject,
                QuestionId = questionRecord.Dto.Id  // Assuming QuestionDto has an Id property
            };

            return View("SingleQuestion", viewModel);
        }

        [HttpPost("ReportQuestion")]
        public IActionResult ReportQuestion(int questionNumber, int id) // 'id' is the question id from the Questions table
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the report with the provided question id.
            _quizService.LogQuestionReport(userId.Value, id, "User reported question");

            TempData["ReportMessage"] = "The question has been reported. Thank you!";

            // Redirect to NextQuestion so the reported question is skipped.
            return RedirectToAction("NextQuestion");
        }
        [HttpGet("start-ai")]
        public async Task<IActionResult> StartAiQuiz()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var generatedQuestions = await _aiQuizService.GenerateSplitAiQuestionsAsync(userId.Value);
            if (generatedQuestions == null || !generatedQuestions.Any())
            {
                return RedirectToAction("Index", "Quizzes");
            }

            var quizSession = new QuizSession
            {
                UserId = userId.Value,
                IsAdaptive = false,
                Questions = generatedQuestions.Select(q => new QuestionRecord
                {
                    Subject = q.Subject,
                    Dto = q,
                    UserCorrect = null
                }).ToList(),
                TimeStarted = DateTime.Now
            };

            HttpContext.Session.SetObject("QuizSession", quizSession);
            return RedirectToAction("NextQuestion");
        }

    }
}
