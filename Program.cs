using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;
using System;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel BEFORE building the app
if (builder.Environment.IsProduction())
{
    // In production, bind to the port specified by the PORT environment variable (default to 8080)
    builder.WebHost.ConfigureKestrel(options =>
    {
        var portString = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        if (int.TryParse(portString, out int port))
        {
            options.ListenAnyIP(port);
        }
        else
        {
            options.ListenAnyIP(8080);
        }
    });
}
else
{
    // In development, bind to HTTPS on localhost:5555 using the local dev certificate
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5555, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}

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

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// In production, we let Cloud Run handle TLS, so no HTTPS redirection is needed.
if (!app.Environment.IsProduction())
{
    // Optionally, if you need HTTPS redirection in development, uncomment this:
    // app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Add a simple health endpoint for testing
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
