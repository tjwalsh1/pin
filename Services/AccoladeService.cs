using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Pinpoint_Quiz.Dtos;
using Pinpoint_Quiz.Models;

namespace Pinpoint_Quiz.Services
{
    public class AccoladeService
    {
        private readonly MySqlDatabase _db;

        public AccoladeService(MySqlDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Checks various conditions for awarding accolades and awards them if not already given.
        /// Returns a list of accolade names that were newly awarded.
        /// </summary>
        public List<string> CheckAndAwardAccolades(int userId, bool retakeMode, int totalCorrect, int totalQuestions)
        {
            var newlyAwarded = new List<string>();

            // 1. Count total quizzes taken using QuizResults
            int totalQuizzes = GetQuizCount(userId);
            if (totalQuizzes == 5)
                AwardAccolade(userId, "Quiz Amateur", "You have taken 5 quizzes!", newlyAwarded);
            if (totalQuizzes == 20)
                AwardAccolade(userId, "Quiz Expert", "You have taken 20 quizzes!", newlyAwarded);
            if (totalQuizzes == 100)
                AwardAccolade(userId, "Quiz Master", "You have taken 100 quizzes!", newlyAwarded);

            // 2. Perfect score on one quiz (assume a quiz has 10 questions total)
            if (totalQuestions == 10 && totalCorrect == 10)
            {
                AwardAccolade(userId, "Smart Cookie", "Perfect score on a quiz!", newlyAwarded);

                // Check if user has earned 10 perfect scores
                int perfectTens = CountPerfectTens(userId);
                if (perfectTens == 10)
                    AwardAccolade(userId, "Genius", "Perfect score on 10 quizzes!", newlyAwarded);
            }

            // 3. If retake mode is true and user has retaken 5 quizzes, award "Perfectionist"
            if (retakeMode)
            {
                int retakes = CountRetakes(userId);
                if (retakes == 5)
                    AwardAccolade(userId, "Perfectionist", "Retaken 5 quizzes!", newlyAwarded);
            }

            // 4. Overall proficiency condition
            var user = GetUserById(userId);
            if (user != null)
            {
                // For example, if overall proficiency reaches at least Grade - 4
                double needed = (user.Grade ?? 0) - 4;
                if (needed < 1)
                    needed = 1;
                if (user.OverallProficiency >= needed)
                    AwardAccolade(userId, "Huge Improvement", "Your overall proficiency has greatly improved!", newlyAwarded);
            }

            return newlyAwarded;
        }

        // Retrieves the total number of quizzes for a user
        private int GetQuizCount(int userId)
        {
            using (var conn = _db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM QuizResults WHERE UserId=@UserId";
                cmd.Parameters.AddWithValue("@UserId", userId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Counts quizzes where a perfect score was achieved (assumes quiz totals sum to 10)
        private int CountPerfectTens(int userId)
        {
            using (var conn = _db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*)
                    FROM QuizResults
                    WHERE UserId=@UserId 
                      AND (MathTotal + EbrwTotal)=10 
                      AND (MathCorrect + EbrwCorrect)=10";
                cmd.Parameters.AddWithValue("@UserId", userId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Placeholder for counting retakes; you'll need to implement this logic
        private int CountRetakes(int userId)
        {
            // For example, if you track retakes in a separate table or flag, query that.
            return 0;
        }

        // Inserts an accolade if not already awarded, and adds its name to the list.
        private void AwardAccolade(int userId, string name, string description, List<string> newlyAwarded)
        {
            if (!AlreadyHasAccolade(userId, name))
            {
                using (var conn = _db.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Accolades (Name, Description, UserId) VALUES (@Name, @Desc, @UserId)";
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Desc", description);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
                newlyAwarded.Add(name);
            }
        }

        // Checks if an accolade already exists for a user.
        private bool AlreadyHasAccolade(int userId, string name)
        {
            using (var conn = _db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Accolades WHERE UserId=@UserId AND Name=@Name";
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Name", name);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        // Retrieves a user from the Users table.
        private User GetUserById(int userId)
        {
            using (var conn = _db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Email, FirstName, LastName, Grade, ProficiencyMath, ProficiencyEbrw, OverallProficiency FROM Users WHERE Id=@UserId";
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Email = reader.GetString(1),
                            FirstName = reader.GetString(2),
                            LastName = reader.GetString(3),
                            Grade = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                            ProficiencyMath = reader.GetDouble(5),
                            ProficiencyEbrw = reader.GetDouble(6),
                            OverallProficiency = reader.GetDouble(7)
                        };
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves accolades for a user.
        /// </summary>
        public List<AccoladeDto> GetAccoladesForUser(int userId)
        {
            var accolades = new List<AccoladeDto>();

            using (var conn = _db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Name, Description
                    FROM Accolades
                    WHERE UserId = @UserId
                    ORDER BY DateEarned ASC
                ";
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        accolades.Add(new AccoladeDto
                        {
                            Name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                            Description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                        });
                    }
                }
            }

            return accolades;
        }
    }
}
