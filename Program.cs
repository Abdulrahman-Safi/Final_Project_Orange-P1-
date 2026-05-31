using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await SeedRolesAsync(scope.ServiceProvider);
    await SeedDefaultAdminAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

static async Task SeedRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { UserRoles.Admin, UserRoles.Owner, UserRoles.User };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static async Task SeedDefaultAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var email = configuration["SeedAdmin:Email"];
    var password = configuration["SeedAdmin:Password"];
    var fullName = configuration["SeedAdmin:FullName"] ?? "System Admin";

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    var admin = await userManager.FindByEmailAsync(email);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to create the default admin user.");
        }
    }

    if (!await userManager.IsInRoleAsync(admin, UserRoles.Admin))
    {
        await userManager.AddToRoleAsync(admin, UserRoles.Admin);
    }
}
