using MySqlConnector;
using Pinpoint_Quiz.Dtos;
using System.Data;

namespace Pinpoint_Quiz.Services;

public class AccountService
{
    private readonly MySqlDatabase _db;

    public AccountService(MySqlDatabase db) => _db = db;

    public bool RegisterUser(RegisterDto dto)
    {
        using var conn = _db.GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Grade, ClassId, SchoolId, UserRole) 
            VALUES (@Email, @PasswordHash, @FirstName, @LastName, @Grade, @ClassId, @SchoolId, @UserRole);
        ";
        cmd.Parameters.AddWithValue("@Email", dto.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(dto.Password));
        cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
        cmd.Parameters.AddWithValue("@LastName", dto.LastName);
        cmd.Parameters.AddWithValue("@Grade", dto.Grade);
        cmd.Parameters.AddWithValue("@ClassId", dto.ClassId);
        cmd.Parameters.AddWithValue("@SchoolId", dto.SchoolId);
        cmd.Parameters.AddWithValue("@UserRole", dto.UserRole);
        return cmd.ExecuteNonQuery() > 0;
    }

    public int? LoginUser(string email, string password)
    {
        using var conn = _db.GetConnection();
        conn.Open();
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
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Users WHERE Id=@Id";
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return new UserDto
            {
                Id = reader.GetInt32("Id"),
                Email = reader.GetString("Email"),
                FirstName = reader.GetString("FirstName"),
                LastName = reader.GetString("LastName"),
                Grade = reader.IsDBNull("Grade") ? null : reader.GetInt32("Grade"),
                ClassId = reader.IsDBNull("ClassId") ? null : reader.GetInt32("ClassId"),
                SchoolId = reader.IsDBNull("SchoolId") ? null : reader.GetInt32("SchoolId"),
                UserRole = reader.GetString("UserRole"),
                ProficiencyMath = reader.GetDouble("ProficiencyMath"),
                ProficiencyEbrw = reader.GetDouble("ProficiencyEbrw"),
                OverallProficiency = reader.GetDouble("OverallProficiency")
            };
        }
        return null;
    }

}
