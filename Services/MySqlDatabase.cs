using MySqlConnector;

namespace Pinpoint_Quiz.Services;

public class MySqlDatabase
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlDatabase> _logger;

    public MySqlDatabase(string connectionString, ILogger<MySqlDatabase> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }


    // Utility to mask password
    private string MaskPassword(string connStr)
    {
        // Replace password value with ****
        return System.Text.RegularExpressions.Regex.Replace(connStr, @"Pwd=([^;]+)", "Pwd=****");
    }
}
