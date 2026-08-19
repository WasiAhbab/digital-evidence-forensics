using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<EvidenceDbContext>(options =>
{
    var sqlServer = builder.Configuration.GetConnectionString("SqlServerConnection");
    if (string.IsNullOrWhiteSpace(sqlServer))
        sqlServer = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION");

    if (!string.IsNullOrWhiteSpace(sqlServer))
        options.UseSqlServer(sqlServer);
    else
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=digital-evidence.db");
});

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHostedService<EvidenceIntegrityMonitor>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "TraceLock.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<EvidenceDbContext>());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
