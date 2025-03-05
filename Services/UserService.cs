using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pinpoint_Quiz.Models;

namespace Pinpoint_Quiz.Services
{
    public class UserService
    {
        private readonly MySqlDatabase _db;
        private readonly ILogger<UserService> _logger;

        public UserService(MySqlDatabase db, ILogger<UserService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        public User GetUserById(int userId)
        {
            User user = null;
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        Id,
                        Email,
                        PasswordHash,
                        FirstName,
                        LastName,
                        Grade,
                        ClassId,
                        SchoolId,
                        ProficiencyMath,
                        ProficiencyEbrw,
                        OverallProficiency,
                        AvgQuizTime,
                        UserRole
                    FROM Users
                    WHERE Id = @Id
                ";
                cmd.Parameters.AddWithValue("@Id", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    user = new User
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
                _logger.LogError("Error in GetUserById: {0}", ex.Message);
            }
            return user;
        }

        /// <summary>
        /// Retrieves all teachers in a given school.
        /// </summary>
        public List<User> GetTeachersBySchool(int schoolId)
        {
            var teachers = new List<User>();
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        Id,
                        Email,
                        PasswordHash,
                        FirstName,
                        LastName,
                        Grade,
                        ClassId,
                        SchoolId,
                        ProficiencyMath,
                        ProficiencyEbrw,
                        OverallProficiency,
                        AvgQuizTime,
                        UserRole
                    FROM Users
                    WHERE SchoolId = @SchoolId AND UserRole = 'Teacher'
                    ORDER BY LastName, FirstName
                ";
                cmd.Parameters.AddWithValue("@SchoolId", schoolId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var teacher = new User
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
                    teachers.Add(teacher);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in GetTeachersBySchool: {0}", ex.Message);
            }
            return teachers;
        }
    }
}
