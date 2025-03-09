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
        var conn = new MySqlConnection(_connectionString);
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
            _logger.LogInformation("Successfully opened connection to MySQL.");
        }
        return conn;
    }


    // Utility to mask password
    private string MaskPassword(string connStr)
    {
        // Replace password value with ****
        return System.Text.RegularExpressions.Regex.Replace(connStr, @"Pwd=([^;]+)", "Pwd=****");
    }
}
