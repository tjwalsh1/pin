using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Pinpoint_Quiz.Dtos;
using Pinpoint_Quiz.Models;

namespace Pinpoint_Quiz.Services
{
    public class QuizService
    {
        private readonly MySqlDatabase _db;
        private readonly ILogger<QuizService> _logger;

        public QuizService(MySqlDatabase db, ILogger<QuizService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // --------------------------------------------
        // 1) QUIZ HISTORY & LOGGING
        // --------------------------------------------

        public List<QuizHistoryRecord> GetLast10Quizzes(int userId)
        {
            var list = new List<QuizHistoryRecord>();
            using var conn = _db.GetConnection(); // Already opened
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            SELECT
                Id,
                UserId,
                QuizDate,
                MathProficiency,
                EbrwProficiency,
                OverallProficiency,
                MathCorrect,
                EbrwCorrect,
                MathTotal,
                EbrwTotal,
                ActualMathProficiency,
                ActualEbrwProficiency,
                ActualOverallProficiency,
                TimeElapsed,
                Questions
            FROM QuizResults
            WHERE UserId = @UserId
            ORDER BY QuizDate DESC
            LIMIT 10
        ";
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var record = new QuizHistoryRecord
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    QuizDate = reader.GetDateTime(2),
                    MathProficiency = reader.GetDouble(3),
                    EbrwProficiency = reader.GetDouble(4),
                    OverallProficiency = reader.GetDouble(5),
                    MathCorrect = reader.GetInt32(6),
                    EbrwCorrect = reader.GetInt32(7),
                    MathTotal = reader.GetInt32(8),
                    EbrwTotal = reader.GetInt32(9),
                    ActualMathProficiency = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
                    ActualEbrwProficiency = reader.IsDBNull(11) ? 0 : reader.GetDouble(11),
                    ActualOverallProficiency = reader.IsDBNull(12) ? 0 : reader.GetDouble(12),
                    TimeElapsed = reader.IsDBNull(13) ? 0 : reader.GetDouble(13)
                };

                // Parse the Questions JSON
                var questionsJson = reader.IsDBNull(14) ? "[]" : reader.GetString(14);
                var parsedQuestions = JsonSerializer.Deserialize<List<QuestionResultDto>>(questionsJson);
                if (parsedQuestions != null)
                {
                    record.QuestionResults = parsedQuestions;
                }

                list.Add(record);
            }
            return list;
        }

        public void LogQuestionReport(int userId, int questionId, string reason)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO QuestionReports (UserId, QuestionId, Reason)
                VALUES (@UserId, @QId, @Reason);
            ";
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@QId", questionId);
            cmd.Parameters.AddWithValue("@Reason", reason);
            cmd.ExecuteNonQuery();
        }

        // --------------------------------------------
        // 2) QUESTION RETRIEVAL
        // --------------------------------------------

        private QuestionDto MapReaderToQuestion(MySqlDataReader reader)
        {
            var difficultyInt = reader.GetInt32(7); // read an int
            double difficulty = difficultyInt;      // cast to double if needed

            return new QuestionDto
            {
                Id = reader.GetInt32(0),
                QuestionPrompt = reader.GetString(1),
                CorrectAnswer = reader.GetString(2),
                WrongAnswers = new List<string>
        {
            reader.IsDBNull(3) ? "" : reader.GetString(3),
            reader.IsDBNull(4) ? "" : reader.GetString(4),
            reader.IsDBNull(5) ? "" : reader.GetString(5),
        },
                Explanation = reader.GetString(6),
                Difficulty = difficulty,         // now it's a double in your DTO
                Subject = reader.GetString(8)
            };
        }


        public QuestionDto GetRandomQuestion(string subject)
        {
            QuestionDto dto = null;
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, question_prompt, correct_answer, wrong_answer1, wrong_answer2, wrong_answer3,
                           explanation, difficulty, subject
                    FROM Questions
                    WHERE subject = @Subj
                    ORDER BY RAND()
                    LIMIT 1;
                ";
                cmd.Parameters.AddWithValue("@Subj", subject);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    dto = MapReaderToQuestion(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetRandomQuestion error: {ex.Message}");
            }
            return dto;
        }



        public QuestionDto GetQuestionByDifficulty(string subject, int difficulty)
        {
            // 1. Attempt an exact subject/difficulty match
            var question = FetchSingleQuestion(subject, difficulty);
            if (question != null)
            {
                return question;
            }

            _logger.LogWarning("No question found for subject={Subject}, difficulty={Diff}, trying fallback...", subject, difficulty);

            // 2. Try an approximate difficulty approach. We'll do a small loop ±1, ±2, etc.
            for (int offset = 1; offset <= 9; offset++)
            {
                int lowerDiff = difficulty - offset;
                int upperDiff = difficulty + offset;

                // try lower if in [1..10]
                if (lowerDiff >= 1)
                {
                    question = FetchSingleQuestion(subject, lowerDiff);
                    if (question != null)
                    {
                        _logger.LogInformation("Fallback found question at difficulty {Diff}", lowerDiff);
                        return question;
                    }
                }

                // try upper if in [1..10]
                if (upperDiff <= 10)
                {
                    question = FetchSingleQuestion(subject, upperDiff);
                    if (question != null)
                    {
                        _logger.LogInformation("Fallback found question at difficulty {Diff}", upperDiff);
                        return question;
                    }
                }
            }

            // 3. If we still have no question, fallback to a random question for that subject
            _logger.LogWarning("No approximate difficulty found. Fallback: random question for {Subject}", subject);
            question = GetRandomQuestion(subject);
            if (question != null)
            {
                return question;
            }

            // 4. As a last resort, we return null, but we log an error
            _logger.LogError("Total fallback failed. No question at all for {Subject}. Table is empty?");
            return null;
        }

        /// <summary>
        /// Helper that fetches a single question for exact subject/difficulty (or returns null).
        /// </summary>
        private QuestionDto FetchSingleQuestion(string subject, int difficulty)
        {
            QuestionDto dto = null;
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT 
                id,
                question_prompt,
                correct_answer,
                wrong_answer1,
                wrong_answer2,
                wrong_answer3,
                explanation,
                difficulty,
                subject
            FROM Questions
            WHERE subject = @Subj
              AND difficulty = @Diff
            ORDER BY RAND()
            LIMIT 1;
        ";
                cmd.Parameters.AddWithValue("@Subj", subject);
                cmd.Parameters.AddWithValue("@Diff", difficulty);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    dto = MapReaderToQuestion(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FetchSingleQuestion error for {Subject} diff={Diff}", subject, difficulty);
            }
            return dto;
        }


        // --------------------------------------------
        // 3) ADAPTIVE QUIZ METHODS
        // --------------------------------------------

        // Returns a question list for an adaptive quiz. 
        // But we want the actual logic from the backup:
        // We'll do it question by question in the controller anyway,
        // but keep this method in case it's used by an API.
        public List<QuestionDto> GenerateAdaptiveQuiz(int userId, string subject, int count)
        {
            // Provide real adaptive logic if you need an entire quiz at once.
            // The old code uses a question-by-question approach in the controller,
            // so this might be a fallback:
            var list = new List<QuestionDto>();
            double userProf = GetUserProficiency(userId, subject);

            // As a placeholder, just pick 'count' questions around userProf. 
            // This is simplified for demonstration; you can refine further.
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    id, question_prompt, correct_answer, wrong_answer1, wrong_answer2, wrong_answer3,
                    explanation, difficulty, subject
                FROM Questions
                WHERE subject = @Subj
                ORDER BY ABS(difficulty - @Prof), RAND()
                LIMIT @Count;
            ";
            cmd.Parameters.AddWithValue("@Subj", subject);
            cmd.Parameters.AddWithValue("@Prof", userProf);
            cmd.Parameters.AddWithValue("@Count", count);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToQuestion(reader));
            }
            return list;
        }

        // If the quiz is non-adaptive (like normal or old style),
        // we just pick random questions. We'll keep it for backward compatibility.
        public List<QuestionDto> GenerateNonAdaptiveQuiz(int userId, string subject, int count)
        {
            var list = new List<QuestionDto>();
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT question_prompt, correct_answer, wrong_answer1, wrong_answer2, wrong_answer3,
                           explanation, difficulty, subject
                    FROM Questions
                    WHERE subject = @Subject
                    ORDER BY RAND()
                    LIMIT @Count;
                ";
                cmd.Parameters.AddWithValue("@Subject", subject);
                cmd.Parameters.AddWithValue("@Count", count);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new QuestionDto
                    {
                        QuestionPrompt = reader.GetString(0),
                        CorrectAnswer = reader.GetString(1),
                        WrongAnswers = new List<string>
                        {
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4)
                        },
                        Explanation = reader.GetString(5),
                        Difficulty = reader.GetDouble(6),
                        Subject = reader.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GenerateNonAdaptiveQuiz error: {ex.Message}");
            }
            return list;
        }

        // --------------------------------------------
        // 4) UTILITY: GET/SET PROFICIENCY
        // --------------------------------------------

        public double GetUserProficiency(int userId, string subject)
        {
            double proficiency = 1.0;
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            if (subject == "Math")
                cmd.CommandText = "SELECT ProficiencyMath FROM Users WHERE Id = @UId";
            else
                cmd.CommandText = "SELECT ProficiencyEbrw FROM Users WHERE Id = @UId";

            cmd.Parameters.AddWithValue("@UId", userId);
            var result = cmd.ExecuteScalar();
            if (result != null && double.TryParse(result.ToString(), out double val))
            {
                proficiency = val;
            }

            // Minimum proficiency is 1.0
            if (proficiency < 1.0) proficiency = 1.0;
            return proficiency;
        }

        // The adaptive logic for actual proficiency updates (like final after a quiz).
        public void UpdateActualProficiency(int userId, List<QuestionResultDto> questionResults, bool retakeMode)
        {
            double mathProf, ebrwProf;
            using (var conn = _db.GetConnection())
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ProficiencyMath, ProficiencyEbrw FROM Users WHERE Id = @U";
                cmd.Parameters.AddWithValue("@U", userId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    mathProf = reader.GetDouble(0);
                    ebrwProf = reader.GetDouble(1);
                }
                else
                {
                    _logger.LogWarning($"UpdateActualProficiency: user {userId} not found.");
                    return;
                }
            }

            foreach (var qr in questionResults)
            {
                if (qr.Subject == "Math")
                    mathProf = ApplyProficiencyChange(mathProf, qr.Difficulty, qr.IsCorrect, retakeMode);
                else if (qr.Subject == "EBRW")
                    ebrwProf = ApplyProficiencyChange(ebrwProf, qr.Difficulty, qr.IsCorrect, retakeMode);
            }

            mathProf = Math.Clamp(Math.Round(mathProf, 2), 1.0, 10.0);
            ebrwProf = Math.Clamp(Math.Round(ebrwProf, 2), 1.0, 10.0);
            double overall = Math.Round((mathProf + ebrwProf) / 2.0, 2);

            using (var conn = _db.GetConnection())
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Users
                    SET ProficiencyMath = @M,
                        ProficiencyEbrw = @E,
                        OverallProficiency = @O
                    WHERE Id = @U
                ";
                cmd.Parameters.AddWithValue("@M", mathProf);
                cmd.Parameters.AddWithValue("@E", ebrwProf);
                cmd.Parameters.AddWithValue("@O", overall);
                cmd.Parameters.AddWithValue("@U", userId);
                cmd.ExecuteNonQuery();
            }
        }

        // This is the function that we’ll modify so actual proficiency changes 
        // more slowly (or differently) than the user’s “estimated” proficiency.
        private double ApplyProficiencyChange(double currentProf, double questionDifficulty, bool isCorrect, bool retakeMode)
        {
            // We'll keep it simple: compare currentProf to questionDifficulty 
            // to see if the question is "above," "equal," or "below" the user's proficiency.
            // Then apply the increments/decrements you specified.

            // Because "equal" can be ambiguous, define a small range for "at the same level."
            const double epsilon = 0.2;
            double diff = currentProf - questionDifficulty;

            if (isCorrect)
            {
                // CORRECT
                if (diff < -epsilon)
                {
                    // If questionDifficulty is significantly above currentProf
                    // => +0.06
                    return currentProf + 0.06;
                }
                else if (Math.Abs(diff) <= epsilon)
                {
                    // “At” the same approximate level
                    // => +0.04
                    return currentProf + 0.04;
                }
                else
                {
                    // Otherwise, it's below the current proficiency
                    // => +0.02
                    return currentProf + 0.02;
                }
            }
            else
            {
                // INCORRECT
                if (diff < -epsilon)
                {
                    // If questionDifficulty is significantly above currentProf
                    // => -0.02
                    return currentProf - 0.02;
                }
                else if (Math.Abs(diff) <= epsilon)
                {
                    // “At” the same approximate level
                    // => -0.06
                    return currentProf - 0.06;
                }
                else
                {
                    // The question is below user’s proficiency
                    // => -0.12
                    return currentProf - 0.12;
                }
            }
        }


        public (double math, double ebrw, double overall) GetActualProficiencies(int userId)
        {
            double math = 0, ebrw = 0, overall = 0;
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ProficiencyMath, ProficiencyEbrw, OverallProficiency FROM Users WHERE Id = @U";
            cmd.Parameters.AddWithValue("@U", userId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                math = reader.GetDouble(0);
                ebrw = reader.GetDouble(1);
                overall = reader.GetDouble(2);
            }
            return (math, ebrw, overall);
        }

        // --------------------------------------------
        // 5) BUILDING THE QUIZ & DIFFICULTY OFFSETS
        // --------------------------------------------

        // For each question in the "easy quiz," 
        // we start 2 below their subject proficiency (clamped >= 1).
        // Normal = exactly their proficiency 
        // Hard = +2 (clamped <= 10)
        // Then we adapt the difficulty up/down as they answer.
        // The final logic is typically done in the controller (like NextQuestion).

        // We keep these methods so you can reference them from your controller.
        public int GetStartingDifficulty(double subjectProf, DifficultyMode mode)
        {
            // Round the user proficiency first
            double p = Math.Round(subjectProf, 0);
            switch (mode)
            {
                case DifficultyMode.Easy:
                    p = p - 2;
                    break;
                case DifficultyMode.Hard:
                    p = p + 2;
                    break;
                default: // Normal
                    // p = p;
                    break;
            }
            if (p < 1) p = 1;
            if (p > 10) p = 10;
            return (int)Math.Round(p, 0);
        }

        // --------------------------------------------
        // 6) QUIZ SUBMISSION / RESULTS
        // --------------------------------------------

        public bool SubmitQuiz(int studentId, int quizId, QuizSubmissionDto submission)
        {
            // This method can remain a stub or do real logic.
            _logger.LogInformation($"API SubmitQuiz: student {studentId}, quiz {quizId}.");
            return true;
        }

        public void SaveQuestionResponse(int userId, int quizId, string questionPrompt, string selectedAnswer, bool isCorrect)
        {
            // For demonstration only. You can store them in a table if you want.
            _logger.LogInformation($"Saving question response: User={userId}, Quiz={quizId}, Prompt={questionPrompt}, Answer={selectedAnswer}, Correct={isCorrect}");
        }

        // --------------------------------------------
        // 7) QUIZ RESULTS
        // --------------------------------------------

        public int SaveQuizResults(
            int studentId,
            double mathProf,
            double ebrwProf,
            double overallProf,
            int mathCorrect,
            int ebrwCorrect,
            int mathTotal,
            int ebrwTotal,
            List<QuestionResultDto> questionResults,
            double actualMath,
            double actualEbrw,
            double actualOverall,
            DateTime timeStarted,
            DateTime timeEnded,
            double timeElapsed)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            string questionsJson = JsonSerializer.Serialize(questionResults);

            cmd.CommandText = @"
                INSERT INTO QuizResults 
                    (UserId, QuizDate, MathProficiency, EbrwProficiency, OverallProficiency,
                     MathCorrect, EbrwCorrect, MathTotal, EbrwTotal, Questions,
                     ActualMathProficiency, ActualEbrwProficiency, ActualOverallProficiency,
                     TimeStarted, TimeEnded, TimeElapsed)
                VALUES
                    (@UserId, NOW(), @MathProf, @EbrwProf, @OverallProf,
                     @MathCorrect, @EbrwCorrect, @MathTotal, @EbrwTotal, @Questions,
                     @ActualMath, @ActualEbrw, @ActualOverall,
                     @TimeStarted, @TimeEnded, @TimeElapsed);

                SELECT LAST_INSERT_ID();
            ";

            cmd.Parameters.AddWithValue("@UserId", studentId);
            cmd.Parameters.AddWithValue("@MathProf", mathProf);
            cmd.Parameters.AddWithValue("@EbrwProf", ebrwProf);
            cmd.Parameters.AddWithValue("@OverallProf", overallProf);
            cmd.Parameters.AddWithValue("@MathCorrect", mathCorrect);
            cmd.Parameters.AddWithValue("@EbrwCorrect", ebrwCorrect);
            cmd.Parameters.AddWithValue("@MathTotal", mathTotal);
            cmd.Parameters.AddWithValue("@EbrwTotal", ebrwTotal);
            cmd.Parameters.AddWithValue("@Questions", questionsJson);
            cmd.Parameters.AddWithValue("@ActualMath", actualMath);
            cmd.Parameters.AddWithValue("@ActualEbrw", actualEbrw);
            cmd.Parameters.AddWithValue("@ActualOverall", actualOverall);
            cmd.Parameters.AddWithValue("@TimeStarted", timeStarted);
            cmd.Parameters.AddWithValue("@TimeEnded", timeEnded);
            cmd.Parameters.AddWithValue("@TimeElapsed", timeElapsed);

            int quizId = Convert.ToInt32(cmd.ExecuteScalar());
            _logger.LogInformation($"Saved quiz results with Quiz ID: {quizId}");
            return quizId;
        }

        public QuizResults GetQuizResults(int userId, int quizId)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    Questions,
                    MathCorrect,
                    EbrwCorrect,
                    MathTotal,
                    EbrwTotal,
                    MathProficiency,
                    EbrwProficiency,
                    OverallProficiency,
                    ActualMathProficiency,
                    ActualEbrwProficiency,
                    ActualOverallProficiency,
                    QuizDate
                FROM QuizResults
                WHERE UserId = @UserId
                  AND Id = @QuizId
            ";
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@QuizId", quizId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string questionsJson = reader.IsDBNull(0) ? "[]" : reader.GetString(0);

                var quizResults = new QuizResults
                {
                    MathCorrect = reader.GetInt32(1),
                    EbrwCorrect = reader.GetInt32(2),
                    MathTotal = reader.GetInt32(3),
                    EbrwTotal = reader.GetInt32(4),
                    FinalProficiencyMath = reader.GetDouble(5),
                    FinalProficiencyEbrw = reader.GetDouble(6),
                    FinalOverallProficiency = reader.GetDouble(7),
                    ActualMathProficiency = reader.GetDouble(8),
                    ActualEbrwProficiency = reader.GetDouble(9),
                    ActualOverallProficiency = reader.GetDouble(10),
                    QuizDate = reader.GetDateTime(11)
                };

                quizResults.QuestionResults = JsonSerializer
                    .Deserialize<List<QuestionResultDto>>(questionsJson)
                    ?? new List<QuestionResultDto>();

                return quizResults;
            }
            return null;
        }

        public QuizResults GetLatestQuizResult(int userId)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    Questions,
                    MathCorrect,
                    EbrwCorrect,
                    MathTotal,
                    EbrwTotal,
                    MathProficiency,
                    EbrwProficiency,
                    OverallProficiency,
                    ActualMathProficiency,
                    ActualEbrwProficiency,
                    ActualOverallProficiency,
                    QuizDate
                FROM QuizResults
                WHERE UserId = @UserId
                ORDER BY QuizDate DESC
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string questionsJson = reader.IsDBNull(0) ? "[]" : reader.GetString(0);

                var quizResults = new QuizResults
                {
                    MathCorrect = reader.GetInt32(1),
                    EbrwCorrect = reader.GetInt32(2),
                    MathTotal = reader.GetInt32(3),
                    EbrwTotal = reader.GetInt32(4),
                    FinalProficiencyMath = reader.GetDouble(5),
                    FinalProficiencyEbrw = reader.GetDouble(6),
                    FinalOverallProficiency = reader.GetDouble(7),
                    ActualMathProficiency = reader.IsDBNull(8) ? 0 : reader.GetDouble(8),
                    ActualEbrwProficiency = reader.IsDBNull(9) ? 0 : reader.GetDouble(9),
                    ActualOverallProficiency = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
                    QuizDate = reader.GetDateTime(11)
                };

                quizResults.QuestionResults = System.Text.Json.JsonSerializer
                    .Deserialize<List<QuestionResultDto>>(questionsJson)
                    ?? new List<QuestionResultDto>();

                return quizResults;
            }
            return null;
        }
        // In QuizService or similar
        public int GetTotalWrongAnswers(int userId)
        {
            // Example if you store quiz submissions in a 'QuizHistoryRecord' or 'QuizResults' table:
            // Each record might have question-level detail. Suppose you keep them in a JSON or separate table. 
            // The logic below depends on how you actually store them.

            // Pseudocode:
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT COUNT(*) 
        FROM QuizSubmissions 
        WHERE UserId = @UserId
          AND IsCorrect = 0;
    ";
            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

    }
}
