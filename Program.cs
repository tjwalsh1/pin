using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using MySqlConnector; // Ensure this NuGet package is added
using System;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// MySQL connection string for Cloud SQL
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "34.121.132.237";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "mydatabase";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root"; // Replace with your DB user
var dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? "your-password"; // Replace with your password

string dbConnectionString = $"Server={dbHost};Database={dbName};User={dbUser};Password={dbPass};SslMode=None;";

// Register MySQL connection explicitly
builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(dbConnectionString));
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();

// MVC and Session configuration
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Correct port configuration (Cloud Run)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
