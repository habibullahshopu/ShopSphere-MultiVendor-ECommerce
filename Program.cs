using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews();

// =====================================================
// Razor Pages
// Required for ASP.NET Core Identity
// =====================================================

builder.Services.AddRazorPages();

// =====================================================
// Database
// =====================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));





// =====================================================
// ASP.NET Core Identity + Roles
// =====================================================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// =====================================================
// SSLCOMMERZ
// =====================================================

builder.Services.Configure<SSLCOMMERZSettings>(
    builder.Configuration.GetSection("SSLCOMMERZ"));

builder.Services.AddHttpClient<
    ISSLCOMMERZService,
    SSLCOMMERZService>();

// =====================================================
// Application Cookie
// =====================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});


builder.Services.AddSingleton<IAISearchService>(sp =>
{
    var configuration =
           sp.GetRequiredService<IConfiguration>();

    var apiKey =
        configuration["OpenAI:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "OpenAI API key is not configured.");
    }

    var model =
        configuration["OpenAI:Model"]
        ?? "gpt-5.2";

    return new OpenAISearchService(
        apiKey,
        model);
});
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<SalesDataService>();
builder.Services.AddScoped<InventoryDataService>();
builder.Services.AddSingleton<IAISalesPredictionService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var apiKey = configuration["OpenAI:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "OpenAI API key is not configured.");
    }
    var model = configuration["OpenAI:Model"] ?? "gpt-5.2";

    return new OpenAISalesPredictionService(apiKey, model);
});

builder.Services.AddSingleton<IAIInventoryPredictionService>(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var apiKey =
        configuration["OpenAI:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "OpenAI API key is not configured.");
    }

    var model =
        configuration["OpenAI:Model"]
        ?? "gpt-5.2";

    return new OpenAIInventoryPredictionService(
        apiKey,
        model);
});
// =====================================================
// BUILD APP
// =====================================================

var app = builder.Build();

// =====================================================
// DEFAULT ADMIN + ROLES SEEDING
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    // =================================================
    // ADMIN ROLE
    // =================================================

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        var result =
            await roleManager.CreateAsync(
                new IdentityRole("Admin"));

        if (!result.Succeeded)
        {
            throw new Exception(
                "Failed to create Admin role.");
        }
    }

    // =================================================
    // CUSTOMER ROLE
    // =================================================

    if (!await roleManager.RoleExistsAsync("Customer"))
    {
        var result =
            await roleManager.CreateAsync(
                new IdentityRole("Customer"));

        if (!result.Succeeded)
        {
            throw new Exception(
                "Failed to create Customer role.");
        }
    }

    // =================================================
    // VENDOR ROLE
    // =================================================

    if (!await roleManager.RoleExistsAsync("Vendor"))
    {
        var result =
            await roleManager.CreateAsync(
                new IdentityRole("Vendor"));

        if (!result.Succeeded)
        {
            throw new Exception(
                "Failed to create Vendor role.");
        }
    }

    // =================================================
    // DEFAULT ADMIN ACCOUNT
    // =================================================

    string adminEmail =
        "admin@shopsphere.com";

    string adminPassword =
        "Admin123";

    var adminUser =
        await userManager.FindByEmailAsync(
            adminEmail);

    // =================================================
    // CREATE ADMIN IF NOT EXISTS
    // =================================================

    if (adminUser == null)
    {
        var newAdmin = new ApplicationUser
        {
            FullName =
                "ShopSphere Administrator",

            UserName =
                adminEmail,

            Email =
                adminEmail,

            EmailConfirmed =
                true,

            IsActive =
                true,

            CreatedAt =
                DateTime.UtcNow
        };

        var createResult =
            await userManager.CreateAsync(
                newAdmin,
                adminPassword);

        if (!createResult.Succeeded)
        {
            var errors =
                string.Join(
                    ", ",
                    createResult.Errors
                        .Select(e => e.Description));

            throw new Exception(
                $"Failed to create default admin: {errors}");
        }

        var roleResult =
            await userManager.AddToRoleAsync(
                newAdmin,
                "Admin");

        if (!roleResult.Succeeded)
        {
            throw new Exception(
                "Failed to assign Admin role.");
        }
    }
    else
    {
        // =================================================
        // EXISTING ADMIN
        // =================================================

        if (!adminUser.IsActive)
        {
            adminUser.IsActive = true;

            await userManager.UpdateAsync(
                adminUser);
        }

        if (!await userManager.IsInRoleAsync(
                adminUser,
                "Admin"))
        {
            await userManager.AddToRoleAsync(
                adminUser,
                "Admin");
        }
    }
}

// =====================================================
// HTTP REQUEST PIPELINE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// STATIC ASSETS
// =====================================================

app.MapStaticAssets();

// =====================================================
// ROOT URL
// / → Customer Home
// =====================================================

app.MapControllerRoute(
    name: "customerHome",
    pattern: "",
    defaults: new
    {
        area = "Customer",
        controller = "Home",
        action = "Index"
    })
    .WithStaticAssets();

// =====================================================
// AREA ROUTE
// =====================================================

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// =====================================================
// DEFAULT ROUTE
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// =====================================================
// IDENTITY RAZOR PAGES
// =====================================================

app.MapRazorPages();

// =====================================================
// RUN
// =====================================================

app.Run();