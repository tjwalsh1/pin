using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using System;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// Setup Serilog for logging
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
builder.Services.AddHttpClient<AIQuizService>();
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

// Authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = "<Your Google Client ID>";
    options.ClientSecret = "<Your Google Client Secret>";
    options.Scope.Add("https://www.googleapis.com/auth/classroom.courses");
    options.Scope.Add("https://www.googleapis.com/auth/classroom.coursework.students");
    options.SaveTokens = true;
});

var app = builder.Build();

// Use developer exception page in Development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Force Kestrel to always listen on port 8080
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);  // Hardcoded port 8080
});

// Optional: Log that we're binding to port 8080
Log.Information("Kestrel is configured to listen on port 8080.");

// Initialize database (wrap in try-catch if desired)
InitializeDatabase(app.Services);

// Do NOT use HTTPS redirection in production since Cloud Run terminates TLS
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

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
