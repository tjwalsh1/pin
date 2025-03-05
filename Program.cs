using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using Microsoft.Data.Sqlite;
using System;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Correct SQLite connection string for Cloud Run
var dbPath = builder.Environment.IsDevelopment() ? "Qs9.db" : "/tmp/Qs9.db";
var connString = $"Data Source={dbPath}";

// Register SQLiteDatabase explicitly
builder.Services.AddSingleton(new SQLiteDatabase(connString));

// Explicitly register IHttpContextAccessor (critical fix)
builder.Services.AddHttpContextAccessor();

// Register your custom services correctly
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();

// MVC and session
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Ensure app listens on PORT provided by Cloud Run
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

// SQLite Initialization (Safe, Fast, and Explicit)
InitializeSQLiteDatabase(dbPath);

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Method: Guaranteed Correct SQLite initialization
void InitializeSQLiteDatabase(string databasePath)
{
    if (!File.Exists(dbPath))
    {
        try
        {
            using var connection = new SqliteConnection(connString);
            connection.Open();

            var commandText = @"
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
                    CreatedAt DATE DEFAULT CURRENT_TIMESTAMP
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

            using var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            cmd.ExecuteNonQuery();

            Log.Information("SQLite database initialized successfully at {DatabasePath}", databasePath);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to initialize SQLite database");
            throw;
        }
    }
}
