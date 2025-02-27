using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using System;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog logging to output to the console.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Determine which connection string to use.
// In development, we use the local file (Qs9.db) from the repository.
// In production (Cloud Run), use /tmp because the container filesystem is read-only.
string dbConnectionString = builder.Environment.IsDevelopment()
    ? "Data Source=Qs9.db"
    : "Data Source=/tmp/Qs9.db";

// Register custom services.
// The SQLiteDatabase service should expose a GetConnection method that returns a SqliteConnection.
builder.Services.AddSingleton<SQLiteDatabase>(_ => new SQLiteDatabase(dbConnectionString));
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();

// Register IHttpContextAccessor and MVC services.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Configure session services.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure port binding and protocol based on environment.
if (builder.Environment.IsDevelopment())
{
    // In development, listen on HTTPS at port 5555.
    builder.WebHost.UseUrls("https://0.0.0.0:5555");
}
else
{
    // In production (Cloud Run), the PORT environment variable is provided.
    // Explicitly set ASPNETCORE_URLS and bind to 0.0.0.0 on that port.
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

// Initialize the SQLite database.
// In production, this creates /tmp/Qs9.db (or uses it if it already exists).
InitializeDatabase(app.Services);

app.UseStaticFiles();
app.UseRouting();

// In production, Cloud Run terminates TLS so HTTPS redirection is unnecessary.
// In development, if you're using HTTPS you might enable redirection as needed.
// app.UseHttpsRedirection();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// This method initializes the SQLite database by opening a connection
// and executing SQL to create tables if they don't exist.
static void InitializeDatabase(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SQLiteDatabase>();

        using var connection = db.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Grade INTEGER,
                ClassId INTEGER,
                SchoolId INTEGER,
                ProficiencyMath REAL DEFAULT 1.0,
                ProficiencyEbrw REAL DEFAULT 1.0,
                OverallProficiency REAL DEFAULT 1.0,
                AvgQuizTime REAL DEFAULT 0.0,
                UserRole TEXT NOT NULL DEFAULT 'Student'
            );
            CREATE TABLE IF NOT EXISTS Performances (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Week DATE NOT NULL,
                ProficiencyMath REAL NOT NULL,
                ProficiencyEbrw REAL NOT NULL,
                OverallProficiency REAL NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );
            CREATE TABLE IF NOT EXISTS Questions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                subject TEXT NOT NULL,
                difficulty INTEGER NOT NULL,
                passage TEXT,
                question_prompt TEXT NOT NULL,
                correct_answer TEXT NOT NULL,
                wrong_answer1 TEXT NOT NULL,
                wrong_answer2 TEXT NOT NULL,
                wrong_answer3 TEXT NOT NULL,
                explanation TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Quizzes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                CreatedAt DATE NOT NULL DEFAULT (datetime('now'))
            );
            CREATE TABLE IF NOT EXISTS QuizSubmissions (
                SubmissionId INTEGER PRIMARY KEY AUTOINCREMENT,
                QuizId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                QuestionId INTEGER NOT NULL,
                SelectedAnswer TEXT NOT NULL,
                IsCorrect INTEGER NOT NULL CHECK (IsCorrect IN (0, 1)),
                FOREIGN KEY (QuizId) REFERENCES Quizzes(Id),
                FOREIGN KEY (UserId) REFERENCES Users(Id),
                FOREIGN KEY (QuestionId) REFERENCES Questions(id)
            );
        ";
        command.ExecuteNonQuery();
        Log.Information("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed.");
        throw;
    }
}
