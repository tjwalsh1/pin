using Microsoft.AspNetCore.Mvc;
using System;
using Pinpoint_Quiz.Services;

namespace Pinpoint_Quiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly SQLiteDatabase _database;

        public DatabaseController(SQLiteDatabase database)
        {
            _database = database;
        }

        // Example snippet to manually re-init DB
        [HttpPost("initialize")]
        public IActionResult InitializeDatabase()
        {
            try
            {
                using var connection = _database.GetConnection();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Grade INTEGER,
                        ClassId INTEGER,
                        SchoolId INTEGER,
                        ProficiencyMath REAL DEFAULT 1.0,
                        ProficiencyEbrw REAL DEFAULT 1.0,
                        OverallProficiency REAL DEFAULT 1.0,
                        AvgQuizTime REAL DEFAULT 0.0,
                        UserRole TEXT NOT NULL DEFAULT 'Student'
                    );

                    CREATE TABLE IF NOT EXISTS Performances (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Week DATE NOT NULL,
                        ProficiencyMath REAL NOT NULL,
                        ProficiencyEbrw REAL NOT NULL,
                        OverallProficiency REAL NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Questions (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        subject TEXT NOT NULL,
                        difficulty INTEGER NOT NULL,
                        passage TEXT,
                        question_prompt TEXT NOT NULL,
                        correct_answer TEXT NOT NULL,
                        wrong_answer1 TEXT NOT NULL,
                        wrong_answer2 TEXT NOT NULL,
                        wrong_answer3 TEXT NOT NULL,
                        explanation TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Quizzes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        CreatedAt DATE NOT NULL DEFAULT (datetime('now'))
                    );

                    CREATE TABLE IF NOT EXISTS QuizSubmissions (
                        SubmissionId INTEGER PRIMARY KEY AUTOINCREMENT,
                        QuizId INTEGER NOT NULL,
                        UserId INTEGER NOT NULL,
                        QuestionId INTEGER NOT NULL,
                        SelectedAnswer TEXT NOT NULL,
                        IsCorrect INTEGER NOT NULL CHECK (IsCorrect IN (0, 1)),
                        FOREIGN KEY (QuizId) REFERENCES Quizzes(Id),
                        FOREIGN KEY (UserId) REFERENCES Users(Id),
                        FOREIGN KEY (QuestionId) REFERENCES Questions(Id)
                    );

                    -- Additional tables as needed...
                ";
                command.ExecuteNonQuery();

                return Ok("Database initialized.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Initialization failed: {ex.Message}");
            }
        }
    }
}
