using MySqlConnector;
using Pinpoint_Quiz.Dtos;

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
}
