using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using System;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Optional: Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Register custom services
builder.Services.AddSingleton<SQLiteDatabase>(_ => new SQLiteDatabase("Qs9.db"));
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();

// Register IHttpContextAccessor and MVC services
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Session configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cloud Run port binding: read PORT env variable (default to 8080)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Use developer exception page in non-production environments
if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

// Initialize the SQLite database (this will create tables if they don't exist)
InitializeDatabase(app.Services);

app.UseStaticFiles();
app.UseRouting();

// Disable HTTPS redirection if Cloud Run handles TLS termination.
// app.UseHttpsRedirection();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// Initialization method that opens Qs9.db and creates tables if needed.
static void InitializeDatabase(IServiceProvider services)
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
}
