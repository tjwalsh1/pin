﻿using MySqlConnector;
using Pinpoint_Quiz.Dtos;
using System.Data;

namespace Pinpoint_Quiz.Services;

public class AccountService
{
    private readonly MySqlDatabase _db;
    private readonly ILogger<QuizService> _logger;

    public AccountService(MySqlDatabase db, ILogger<QuizService> logger)
    {
        _db = db;
        _logger = logger;
    }
    private string GenerateUsername(RegisterDto dto)
    {
        // Example: use the email prefix as the username.
        var parts = dto.Email.Split('@');
        return parts[0];
    }

    public bool RegisterUser(RegisterDto dto)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open(); // Open the connection before executing commands

            var cmd = conn.CreateCommand();

            double initialProf = dto.Grade - 6.00;
            if (initialProf < 1) initialProf = 1.00;

            cmd.CommandText = @"
            INSERT INTO Users 
            (Email, Username, PasswordHash, FirstName, LastName, Grade, ClassId, SchoolId, 
             ProficiencyMath, ProficiencyEbrw, OverallProficiency, UserRole)
            VALUES
            (@Email, @Username, @PasswordHash, @FirstName, @LastName, @Grade, @ClassId, @SchoolId,
             @ProficiencyMath, @ProficiencyEbrw, @OverallProficiency, @UserRole);
        ";

            cmd.Parameters.AddWithValue("@Email", dto.Email);
            cmd.Parameters.AddWithValue("@Username", GenerateUsername(dto));
            cmd.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(dto.Password));
            cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", dto.LastName);
            cmd.Parameters.AddWithValue("@Grade", dto.Grade);
            cmd.Parameters.AddWithValue("@ClassId", dto.ClassId);
            cmd.Parameters.AddWithValue("@SchoolId", dto.SchoolId);
            cmd.Parameters.AddWithValue("@ProficiencyMath", initialProf);
            cmd.Parameters.AddWithValue("@ProficiencyEbrw", initialProf);
            cmd.Parameters.AddWithValue("@OverallProficiency", initialProf);
            cmd.Parameters.AddWithValue("@UserRole", dto.UserRole);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegisterUser for email: {Email}", dto.Email);
            throw;
        }
    }




    public int? LoginUser(string email, string password)
    {
        using var conn = _db.GetConnection();
        conn.Open(); // Open the connection here

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, PasswordHash FROM Users WHERE Email=@Email";
        cmd.Parameters.AddWithValue("@Email", email);

        using var reader = cmd.ExecuteReader();
        if (reader.Read() && BCrypt.Net.BCrypt.Verify(password, reader.GetString("PasswordHash")))
            return reader.GetInt32("Id");
        return null;
    }

    public UserDto GetUserById(int id)
    {
        using var conn = _db.GetConnection();
        conn.Open(); // Ensure the connection is open
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Users WHERE Id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            // Retrieve the raw value for UserRole and log it
            object rawRoleObj = reader["UserRole"];
            string rawUserRole = rawRoleObj != DBNull.Value ? rawRoleObj.ToString() : null;
            _logger.LogInformation("Retrieved raw UserRole for user {Id}: {RawUserRole}", id, rawUserRole);

            // Use "Student" as the fallback if the value is null or whitespace
            string finalUserRole = string.IsNullOrWhiteSpace(rawUserRole) ? "Student" : rawUserRole;

            return new UserDto
            {
                Id = reader.GetInt32("Id"),
                Email = reader.GetString("Email"),
                FirstName = reader.GetString("FirstName"),
                LastName = reader.GetString("LastName"),
                Grade = reader.IsDBNull("Grade") ? null : reader.GetInt32("Grade"),
                ClassId = reader.IsDBNull("ClassId") ? null : reader.GetInt32("ClassId"),
                SchoolId = reader.IsDBNull("SchoolId") ? null : reader.GetInt32("SchoolId"),
                UserRole = finalUserRole,
                ProficiencyMath = reader.GetDouble("ProficiencyMath"),
                ProficiencyEbrw = reader.GetDouble("ProficiencyEbrw"),
                OverallProficiency = reader.GetDouble("OverallProficiency")
            };
        }
        return null;
    }



}
