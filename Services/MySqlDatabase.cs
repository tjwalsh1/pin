using MySqlConnector;

namespace Pinpoint_Quiz.Services;

public class MySqlDatabase
{
    private readonly string _connectionString;

    public MySqlDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public MySqlConnection GetConnection() => new(_connectionString);
}
