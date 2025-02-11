using System;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using Pinpoint_Quiz.Dtos;
using Pinpoint_Quiz.Models;

namespace Pinpoint_Quiz.Services
{
    public class AccountService
    {
        private readonly SQLiteDatabase _db;
        private readonly ILogger<AccountService> _logger;

        public AccountService(SQLiteDatabase db, ILogger<AccountService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public bool RegisterUser(RegisterDto dto)
        {
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Users 
                    (Email, PasswordHash, FirstName, LastName, Grade, ClassId, SchoolId,
                     ProficiencyMath, ProficiencyEbrw, OverallProficiency, UserRole)
                    VALUES
                    (@Email, @PasswordHash, @FirstName, @LastName, @Grade, @ClassId, @SchoolId,
                     @Math, @Ebrw, @Overall, @UserRole)
                ";

                cmd.Parameters.AddWithValue("@Email", dto.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(dto.Password));
                cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
                cmd.Parameters.AddWithValue("@LastName", dto.LastName);
                cmd.Parameters.AddWithValue("@Grade", dto.Grade);
                cmd.Parameters.AddWithValue("@ClassId", dto.ClassId);
                cmd.Parameters.AddWithValue("@SchoolId", dto.SchoolId);

                double baseProf = dto.Grade - 6;
                if (baseProf < 1.0) baseProf = 1.0;

                cmd.Parameters.AddWithValue("@Math", baseProf);
                cmd.Parameters.AddWithValue("@Ebrw", baseProf);
                cmd.Parameters.AddWithValue("@Overall", baseProf);
                cmd.Parameters.AddWithValue("@UserRole", dto.UserRole ?? "Student");

                cmd.ExecuteNonQuery();
                _logger.LogInformation($"User {dto.Email} registered successfully with base proficiency {baseProf}.");
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                // Unique constraint on Email
                _logger.LogWarning($"RegisterUser: Email {dto.Email} already in use.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RegisterUser error: {ex.Message}");
                return false;
            }
        }

        public int? LoginUser(string email, string password)
        {
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Email = @Email";
                cmd.Parameters.AddWithValue("@Email", email);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    string hashedPass = reader.GetString(1);
                    if (BCrypt.Net.BCrypt.Verify(password, hashedPass))
                    {
                        _logger.LogInformation($"User {email} logged in OK. ID={userId}");
                        return userId;
                    }
                    else
                    {
                        _logger.LogWarning("Password mismatch");
                    }
                }
                else
                {
                    _logger.LogWarning("Email not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
            }
            return null;
        }

        public User GetUserById(int userId)
        {
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Email, PasswordHash, FirstName, LastName,
                           Grade, ClassId, SchoolId, ProficiencyMath, ProficiencyEbrw,
                           OverallProficiency, AvgQuizTime, UserRole
                    FROM Users
                    WHERE Id = @UserId
                ";
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        Id = reader.GetInt32(0),
                        Email = reader.GetString(1),
                        PasswordHash = reader.GetString(2),
                        FirstName = reader.GetString(3),
                        LastName = reader.GetString(4),
                        Grade = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        ClassId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                        SchoolId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        ProficiencyMath = reader.GetDouble(8),
                        ProficiencyEbrw = reader.GetDouble(9),
                        OverallProficiency = reader.GetDouble(10),
                        AvgQuizTime = reader.GetDouble(11),
                        UserRole = reader.GetString(12)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetUserById error: {ex.Message}");
            }
            return null;
        }

        public void GrantFirstLoginAccolade(int userId)
        {
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Accolades (Name, Description, UserId)
                SELECT 'First Login!', 'You logged in for the first time!', @UserId
                WHERE NOT EXISTS (
                    SELECT 1 
                    FROM Accolades 
                    WHERE Name = 'First Login!' AND UserId = @UserId
                )
            ";
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();
        }
    }
}
