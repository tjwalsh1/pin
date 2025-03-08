using MySqlConnector;

namespace Pinpoint_Quiz.Services;

public class MySqlDatabase
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public MySqlDatabase(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public MySqlConnection GetConnection()
    {
        var conn = new MySqlConnection(_connectionString);
        try
        {
            conn.Open();
            _logger.LogInformation("Successfully opened connection to MySQL.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open MySQL connection with connection string: {ConnectionString}", MaskPassword(_connectionString));
            throw;
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
