using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Pinpoint_Quiz.Services
{
    public class SQLiteDatabase
    {
        private readonly string _connectionString;

        public SQLiteDatabase(string databaseFile)
        {
            // You can pass the databaseFile into your connection string if you want dynamic naming,
            // or just hard-code "Qs9.db" if you prefer. For example:
            // _connectionString = $"Data Source={databaseFile};Cache=Shared;Mode=ReadWriteCreate;";
            _connectionString = "Data Source=Qs9.db;Cache=Shared;Mode=ReadWriteCreate;";
        }

        public SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();

            return connection;
        }
    }
}
