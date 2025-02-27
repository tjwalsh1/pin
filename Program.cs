using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register services needed by your application.
builder.Services.AddControllersWithViews();

// Register session services and IHttpContextAccessor.
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// Retrieve the port from the environment variable (default to 8080 if not set)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// Configure Kestrel to listen on all network interfaces on the specified port.
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Additional production settings...
}

app.UseStaticFiles();
app.UseRouting();

// Add session middleware before authorization.
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
