using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

class TestApp
{
    static void Main(string[] args)
    {
        // Test SQLite Connection
        TestSQLiteConnection();

        // Start a Minimal HTTP Server
        Console.WriteLine("Starting basic HTTP server...");
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", () => "Hello, world! Your server is running.");

        app.Run("http://localhost:5000");
    }

    static void TestSQLiteConnection()
    {
        var connectionString = "Data Source=Qs9.db;";
        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("SQLite connection established successfully!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite connection failed: {ex.Message}");
        }
    }
}

