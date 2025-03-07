using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using MySqlConnector;
using System;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// MySQL connection string (use environment variables explicitly!)
var connectionString = builder.Environment.IsDevelopment()
    ? "Server=localhost;Database=mydatabase;User=root;Password=yourpassword;"
    : $"Server={Environment.GetEnvironmentVariable("DB_HOST")};Database={Environment.GetEnvironmentVariable("DB_NAME")};User={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};";

// Explicitly register MySqlDatabase
builder.Services.AddSingleton(new MySqlDatabase(connectionString));

// Explicitly register IHttpContextAccessor (critical fix)
builder.Services.AddHttpContextAccessor();

// Register your custom services (fully explicit fix)
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AIQuizService>();
builder.Services.AddHttpClient<AIQuizService>();
builder.Services.AddScoped<AIQuizService>();

// Add MVC and session explicitly
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Explicit port binding for Cloud Run
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

if (!app.Environment.IsProduction())
    app.UseDeveloperExceptionPage();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
