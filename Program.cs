using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pinpoint_Quiz.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

var connectionString = builder.Environment.IsDevelopment()
    ? "Server=localhost;Database=pinpoint;User=root;Password=password;"
    : $"Server={Environment.GetEnvironmentVariable("DB_HOST")};Database={Environment.GetEnvironmentVariable("DB_NAME")};User={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};";

builder.Services.AddSingleton(new MySqlDatabase(connectionString));
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AIQuizService>();
builder.Services.AddScoped<AccoladeService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<ClassPerformanceService>();
builder.Services.AddScoped<SchoolPerformanceService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
