using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using System;

var builder = WebApplication.CreateBuilder(args);

// Optional: Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Register services
builder.Services.AddSingleton<SQLiteDatabase>(_ => new SQLiteDatabase("Qs9.db"));
builder.Services.AddScoped<PerformanceService>();  
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();


builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

// Initialize database (optional, remove if you rely on DatabaseController)
InitializeDatabase(app.Services);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();

void InitializeDatabase(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SQLiteDatabase>();
    using var conn = db.GetConnection();
    using var cmd = conn.CreateCommand();

    // Example: ensure QuizResults table
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS QuizResults (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            QuizDate DATETIME NOT NULL DEFAULT (datetime('now')),
            MathProficiency REAL,
            EbrwProficiency REAL,
            OverallProficiency REAL,
            MathCorrect INTEGER,
            EbrwCorrect INTEGER,
            MathTotal INTEGER,
            EbrwTotal INTEGER,
            Questions TEXT,
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        );
    ";
    cmd.ExecuteNonQuery();
}
