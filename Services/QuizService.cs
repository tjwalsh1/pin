using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pinpoint_Quiz.Dtos;
using Pinpoint_Quiz.Models;
using MySqlConnector;

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
        public List<QuizHistoryRecord> GetLast10Quizzes(int userId)
        {
            var list = new List<QuizHistoryRecord>();
            using var conn = _db.GetConnection();
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
            Questions   -- MAKE SURE YOU SELECT THIS
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

        public void LogQuestionReport(int userId, int id, string reason)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO QuestionReports (UserId, QuestionId, Reason)
        VALUES (@UserId, @QuestionId, @Reason)
    ";
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@QuestionId", id);  // 'id' is the question's ID from the Questions table.
            cmd.Parameters.AddWithValue("@Reason", reason);
            cmd.ExecuteNonQuery();
        }

        // ----------------------------------------------------------------------
        //  QUESTION RETRIEVAL
        // ----------------------------------------------------------------------
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
ORDER BY RANDOM()
LIMIT 1

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
            QuestionDto dto = null;
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
    id,                  -- column 0
    question_prompt,     -- column 1
    correct_answer,      -- column 2
    wrong_answer1,       -- column 3
    wrong_answer2,       -- column 4
    wrong_answer3,       -- column 5
    explanation,         -- column 6
    difficulty,          -- column 7
    subject              -- column 8
FROM Questions
WHERE subject = @Subj
  AND difficulty = @Diff
ORDER BY RANDOM()
LIMIT 1

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
                _logger.LogError($"GetQuestionByDifficulty error: {ex.Message}");
            }
            return dto;
        }

        public List<QuestionDto> GenerateAdaptiveQuiz(int userId, string subject, int count)
        {
            // (Placeholder) For real adaptive logic, you'd vary difficulty. 
            // Below is just random.
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
                    ORDER BY RANDOM()
                    LIMIT @Count
                ";
                cmd.Parameters.AddWithValue("@Subject", subject);
                cmd.Parameters.AddWithValue("@Count", count);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapReaderToQuestion(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GenerateAdaptiveQuiz error: {ex.Message}");
            }
            return list;
        }
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
    double timeElapsed) // in seconds
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
            (@UserId, datetime('now'), @MathProf, @EbrwProf, @OverallProf, 
             @MathCorrect, @EbrwCorrect, @MathTotal, @EbrwTotal, @Questions,
             @ActualMath, @ActualEbrw, @ActualOverall,
             @TimeStarted, @TimeEnded, @TimeElapsed);
        
        SELECT last_insert_rowid();
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



        public List<QuestionDto> GenerateNonAdaptiveQuiz(int userId, string subject, int count)
        {
            // Also random, for demonstration.
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
                    ORDER BY RANDOM()
                    LIMIT @Count
                ";
                cmd.Parameters.AddWithValue("@Subject", subject);
                cmd.Parameters.AddWithValue("@Count", count);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapReaderToQuestion(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GenerateNonAdaptiveQuiz error: {ex.Message}");
            }
            return list;
        }

        private QuestionDto MapReaderToQuestion(MySqlDataReader reader)
        {
            return new QuestionDto
            {
                Id = reader.GetInt32(0), // Read the 'id' from the first column
                QuestionPrompt = reader.GetString(1),
                CorrectAnswer = reader.GetString(2),
                WrongAnswers = new List<string>
        {
            reader.IsDBNull(3) ? "" : reader.GetString(3),
            reader.IsDBNull(4) ? "" : reader.GetString(4),
            reader.IsDBNull(5) ? "" : reader.GetString(5)
        },
                Explanation = reader.GetString(6),
                Difficulty = reader.GetDouble(7),
                Subject = reader.GetString(8)
            };
        }


        // ----------------------------------------------------------------------
        //  SUBMISSION
        // ----------------------------------------------------------------------
        public bool SubmitQuiz(int studentId, int quizId, QuizSubmissionDto submission)
        {
            // If you want to store final stats or something, do it here.
            _logger.LogInformation($"API SubmitQuiz: student {studentId}, quiz {quizId}.");
            return true;
        }

        // Save each question response in some QuizSubmissions table
        public void SaveQuestionResponse(int userId, int quizId, string questionPrompt, string selectedAnswer, bool isCorrect)
        {
            // For demonstration only; you'd need questionId from the actual question row
            // to store it properly in QuizSubmissions table.
            // Example stub:
            _logger.LogInformation($"Saving question response: User={userId}, Quiz={quizId}, Prompt={questionPrompt}, Answer={selectedAnswer}, Correct={isCorrect}");
        }

        public double GetUserProficiency(int userId, string subject)
        {
            // e.g. read from Users table
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
                proficiency = val;

            return proficiency < 1.0 ? 1.0 : proficiency;
        }

        // ----------------------------------------------------------------------
        //  QUIZ HISTORY / RESULTS
        // ----------------------------------------------------------------------
        public int SaveQuizHistory(
            int studentId,
            double mathProf,
            double ebrwProf,
            double overallProf,
            int mathCorrect,
            int ebrwCorrect,
            int mathTotal,
            int ebrwTotal)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO QuizHistory 
                (UserId, QuizDate, MathProficiency, EbrwProficiency, OverallProficiency,
                 MathCorrect, EbrwCorrect, MathTotal, EbrwTotal)
                VALUES
                (@UserId, datetime('now'), @MathProf, @EbrwProf, @OverallProf,
                 @MathCorrect, @EbrwCorrect, @MathTotal, @EbrwTotal);

                SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("@UserId", studentId);
            cmd.Parameters.AddWithValue("@MathProf", mathProf);
            cmd.Parameters.AddWithValue("@EbrwProf", ebrwProf);
            cmd.Parameters.AddWithValue("@OverallProf", overallProf);
            cmd.Parameters.AddWithValue("@MathCorrect", mathCorrect);
            cmd.Parameters.AddWithValue("@EbrwCorrect", ebrwCorrect);
            cmd.Parameters.AddWithValue("@MathTotal", mathTotal);
            cmd.Parameters.AddWithValue("@EbrwTotal", ebrwTotal);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());
            _logger.LogInformation($"Saved quiz history with ID: {newId}");
            return newId;
        }
        /*
        public List<QuizHistoryRecord> GetQuizHistory(int userId)
        {
            var history = new List<QuizHistoryRecord>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, UserId, QuizDate, MathProficiency, EbrwProficiency, OverallProficiency,
                       MathCorrect, EbrwCorrect, MathTotal, EbrwTotal, TimeElapsed
                FROM QuizHistory
                WHERE UserId = @U
                ORDER BY QuizDate DESC
                LIMIT 10
            ";
            cmd.Parameters.AddWithValue("@U", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new QuizHistoryRecord
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
                    TimeElapsed = reader.GetDouble(15)
                });
            }
            return history;
        }*/

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
                // index 0 => Questions
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
                    // If you want a DateTime field in your model:
                    QuizDate = reader.GetDateTime(11)
                };

                quizResults.QuestionResults = JsonSerializer
                    .Deserialize<List<QuestionResultDto>>(questionsJson)
                    ?? new List<QuestionResultDto>();

                return quizResults;
            }


            // If no row found, return null
            return null;
        }


        private double ApplyProficiencyChange(double currentProficiency, double questionDifficulty, bool isCorrect, bool retakeMode)
        {
            const double epsilon = 0.0001; // to check for "equal" difficulty
                                           // If the question's difficulty is approximately equal to the current proficiency
            if (Math.Abs(currentProficiency - questionDifficulty) < epsilon)
            {
                if (retakeMode)
                {
                    return isCorrect ? currentProficiency + 0.015 : currentProficiency - 0.04;
                }
                return isCorrect ? currentProficiency + 0.03 : currentProficiency - 0.08;
            }
            // If the question is below the current proficiency
            else if (questionDifficulty < currentProficiency)
            {
                if (retakeMode)
                {
                    return isCorrect ? currentProficiency + 0.01 : currentProficiency - 0.06;
                }
                return isCorrect ? currentProficiency + 0.02 : currentProficiency - 0.12;
            }
            // If the question is above the current proficiency
            else
            {
                if (retakeMode)
                {
                    return isCorrect ? currentProficiency + 0.025 : currentProficiency - 0.01;
                }
                return isCorrect ? currentProficiency + 0.05 : currentProficiency - 0.02;
            }
        }

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
                    return; // user not found
                }
            }

            foreach (var qr in questionResults)
            {
                if (qr.Subject == "Math")
                    mathProf = ApplyProficiencyChange(mathProf, qr.Difficulty, qr.IsCorrect, retakeMode);
                else if (qr.Subject == "EBRW")
                    ebrwProf = ApplyProficiencyChange(ebrwProf, qr.Difficulty, qr.IsCorrect, retakeMode);
            }

            mathProf = Math.Min(10.0, Math.Max(1.0, Math.Round(mathProf, 2)));
            ebrwProf = Math.Min(10.0, Math.Max(1.0, Math.Round(ebrwProf, 2)));

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

        public QuizResults GetLatestQuizResult(int userId)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT 
            Questions,                    -- index 0
            MathCorrect,                  -- index 1
            EbrwCorrect,                  -- index 2
            MathTotal,                    -- index 3
            EbrwTotal,                    -- index 4
            MathProficiency,              -- index 5
            EbrwProficiency,              -- index 6
            OverallProficiency,           -- index 7
            ActualMathProficiency,        -- index 8
            ActualEbrwProficiency,        -- index 9
            ActualOverallProficiency,     -- index 10
            QuizDate                      -- index 11
        FROM QuizResults
        WHERE UserId = @UserId
        ORDER BY QuizDate DESC
        LIMIT 1
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
                    QuizDate = reader.GetDateTime(11) // Ensure your QuizResults model includes QuizDate.
                };

                quizResults.QuestionResults = System.Text.Json.JsonSerializer.Deserialize<List<QuestionResultDto>>(questionsJson)
                                                ?? new List<QuestionResultDto>();
                return quizResults;
            }
            return null;
        }


    }
}
