using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Pinpoint_Quiz.Services;

namespace Pinpoint_Quiz.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly MySqlDatabase _db;

    public DatabaseController(MySqlDatabase db) => _db = db;

    [HttpPost("initialize")]
    public IActionResult Initialize()
    {
        using var conn = _db.GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Email VARCHAR(255) UNIQUE NOT NULL,
                PasswordHash VARCHAR(255) NOT NULL,
                FirstName VARCHAR(255) NOT NULL,
                LastName VARCHAR(255) NOT NULL,
                Grade INT,
                ClassId INT,
                SchoolId INT,
                ProficiencyMath DOUBLE DEFAULT 1.0,
                ProficiencyEbrw DOUBLE DEFAULT 1.0,
                OverallProficiency DOUBLE DEFAULT 1.0,
                AvgQuizTime DOUBLE DEFAULT 0.0,
                UserRole VARCHAR(50) DEFAULT 'Student'
            );
            CREATE TABLE IF NOT EXISTS Performances (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                UserId INT NOT NULL,
                Week DATE NOT NULL,
                ProficiencyMath DOUBLE,
                ProficiencyEbrw DOUBLE,
                OverallProficiency DOUBLE,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );
            CREATE TABLE IF NOT EXISTS Questions (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Subject VARCHAR(100),
                Difficulty INT,
                Passage TEXT,
                Question_Prompt TEXT,
                Correct_Answer TEXT,
                Wrong_Answer1 TEXT,
                Wrong_Answer2 TEXT,
                Wrong_Answer3 TEXT,
                Explanation TEXT
            );
            CREATE TABLE IF NOT EXISTS Quizzes (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Title VARCHAR(255),
                Description TEXT,
                CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            CREATE TABLE IF NOT EXISTS QuizSubmissions (
                SubmissionId INT AUTO_INCREMENT PRIMARY KEY,
                QuizId INT,
                UserId INT,
                QuestionId INT,
                SelectedAnswer TEXT,
                IsCorrect BOOLEAN,
                FOREIGN KEY(QuizId) REFERENCES Quizzes(Id),
                FOREIGN KEY(UserId) REFERENCES Users(Id),
                FOREIGN KEY(QuestionId) REFERENCES Questions(Id)
            );
            CREATE TABLE IF NOT EXISTS Accolades (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(255),
                Description TEXT,
                UserId INT,
                DateEarned TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            );
        ";
        cmd.ExecuteNonQuery();

        return Ok("Database initialized.");
    }
}
