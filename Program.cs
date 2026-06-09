using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CampusSentinel.Data;
using CampusSentinel.Repositories;
using CampusSentinel.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Allow both Admins and Guards to access the main management area
    options.Conventions.AuthorizeFolder("/Admin", "RequireGuardRole");
    
    // Auth endpoints protection
    options.Conventions.AuthorizePage("/Auth/Register", "RequireAdminRole");
    
    // Specifically restrict creation, editing, and deletion to Administrators only
    options.Conventions.AuthorizePage("/Admin/Students/Create", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Admin/Students/Delete", "RequireAdminRole");
    
    options.Conventions.AuthorizePage("/Admin/Visitors/Create", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Admin/Visitors/Edit", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Admin/Visitors/Delete", "RequireAdminRole");
    
    options.Conventions.AuthorizePage("/Admin/Staff/Create", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Admin/Staff/Delete", "RequireAdminRole");
    
    options.Conventions.AuthorizePage("/Admin/Guards/Index", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Admin/Guards/Edit", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Admin/Guards/Delete", "RequireAdminRole");

    // Challans module — Admin-only (Wardens / Officials)
    options.Conventions.AuthorizeFolder("/Admin/Challans", "RequireAdminRole");

    // Incidents module
    options.Conventions.AuthorizeFolder("/Incidents", "RequireGuardRole");
    options.Conventions.AuthorizePage("/Incidents/Edit", "RequireAdminRole");

    // Schedule module
    options.Conventions.AuthorizeFolder("/Schedule", "RequireGuardRole");
    options.Conventions.AuthorizePage("/Schedule/Index", "RequireGuardRole");
    options.Conventions.AuthorizePage("/Schedule/Create", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Schedule/SwapRequests", "RequireAdminRole");
    options.Conventions.AuthorizePage("/Schedule/WeeklyReport", "RequireAdminRole");
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
<<<<<<< HEAD
    ?? "Data Source=campussentinel.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
=======
    ?? "Server=localhost;Database=CampusSentinel;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
>>>>>>> 0536f83 (Update project)
    options.ConfigureWarnings(warnings => 
        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IChallanService, ChallanService>();

// Register Services
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<ISchedulerService, SchedulerService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ISmsSender, StubSmsSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireGuardRole", policy => policy.RequireRole("SecurityGuard", "Admin"));
});

var app = builder.Build();

// Ensure the database is created (only if it doesn't already exist)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        // Try to ensure the database and all tables are created matching current C# models.
        dbContext.Database.EnsureCreated();

        // Perform a quick query validation check on new and old tables. 
        // If an old database exists with missing tables, it will throw an exception
        // and safely trigger the self-healing recreate block.
        _ = dbContext.Users.FirstOrDefault();
        _ = dbContext.Incidents.FirstOrDefault();
        _ = dbContext.SystemSettings.FirstOrDefault();
    }
    catch (Exception)
    {
        // Self-healing: If there's any database or schema mismatch, drop and recreate it perfectly from scratch
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

<<<<<<< HEAD
=======
    // Execute SQL Server objects schema (Triggers, Functions, SPs, Indexes)
    var sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "Database", "SqlServerSchema_V2.sql");
    if (File.Exists(sqlPath))
    {
        var sqlScript = File.ReadAllText(sqlPath);
        var batches = System.Text.RegularExpressions.Regex.Split(
            sqlScript, 
            @"^\s*GO\s*$", 
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        foreach (var batch in batches)
        {
            if (!string.IsNullOrWhiteSpace(batch))
            {
                dbContext.Database.ExecuteSqlRaw(batch);
            }
        }
    }

>>>>>>> 0536f83 (Update project)
    // Ensure default admins exist
    var existingHassan = dbContext.Users.FirstOrDefault(u => u.Username == "hassan");
    if (existingHassan == null)
    {
        var defaultHassan = new CampusSentinel.Models.Admin
        {
            Username = "hassan",
            PasswordHash = "hassan123", 
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        dbContext.Users.Add(defaultHassan);
        dbContext.SaveChanges();
    }
    else if (existingHassan.PasswordHash != "hassan123")
    {
        existingHassan.PasswordHash = "hassan123";
        dbContext.SaveChanges();
    }

    var existingAdmin = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
    if (existingAdmin == null)
    {
        var defaultAdmin = new CampusSentinel.Models.Admin
        {
            Username = "admin",
            PasswordHash = "admin123", 
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        dbContext.Users.Add(defaultAdmin);
        dbContext.SaveChanges();
    }
    else if (existingAdmin.PasswordHash != "admin123")
    {
        existingAdmin.PasswordHash = "admin123";
        dbContext.SaveChanges();
    }

    // Run Data Seeder for random test data
    var seeder = new CampusSentinel.Services.DataSeeder(dbContext);
    await seeder.SeedAllAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
