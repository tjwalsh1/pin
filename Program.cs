using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using MySqlConnector; // Ensure this package is installed via NuGet
using System;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// MySQL database connection string
string dbConnectionString = builder.Environment.IsDevelopment()
    ? "YourLocalDevConnectionString" // Replace with your local connection string
    : $"Server={Environment.GetEnvironmentVariable("DB_HOST")};Database={Environment.GetEnvironmentVariable("DB_NAME")};User={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};SslMode=None;";

// Register MySQL connection explicitly
builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(dbConnectionString));

// Register custom services
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();

// Important: Add IHttpContextAccessor explicitly
builder.Services.AddHttpContextAccessor();

// Configure MVC and Sessions
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cloud Run Port Binding
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
