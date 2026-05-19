using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartNotes.Data;
using SmartNotes.Models;
using SmartNotes.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SmartNotesDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<SmartNotesDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath    = "/Account/Login";
    options.LogoutPath   = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

// Groq HTTP client — single reusable instance, base timeout for large responses
builder.Services.AddHttpClient("groq", client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddScoped<GroqNotesService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Auto-apply migrations on startup (creates DB if not exists)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartNotesDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
