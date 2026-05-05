using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services;
using TechNova_IT_Solutions.Services.Interfaces;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Middleware;
using TechNova_IT_Solutions.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;

// Configure Serilog for structured logging with sensitive data filtering
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With<SensitiveDataEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/technova-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews(); // Add MVC Controllers
builder.Services.AddRazorPages(); // Keep Razor Pages support

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(120); // Increase timeout to 120 seconds for remote database
        }));

// Configure External API settings
builder.Services.Configure<ExternalApisConfiguration>(
    builder.Configuration.GetSection("ExternalApis"));

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Register application services (Controllers/Business Logic Layer)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IComplianceManagerService, ComplianceManagerService>();
builder.Services.AddScoped<IPolicyReferenceApiService, PolicyReferenceApiService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddTransient<IEmailService, EmailService>();

// Add in-memory cache (used for thread-safe login lockout tracking)
builder.Services.AddMemoryCache();

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Login endpoint rate limiting: 5 requests per 15 minutes per IP
    options.AddPolicy("login", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? 
                       context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                       "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginEndpoint:PermitLimit", 5),
                Window = TimeSpan.Parse(builder.Configuration["RateLimiting:LoginEndpoint:Window"] ?? "00:15:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queueing
            });
    });

    // API endpoints rate limiting: 100 requests per minute per user
    options.AddPolicy("api", context =>
    {
        var userId = context.User?.Identity?.Name ?? 
                    context.Connection.RemoteIpAddress?.ToString() ?? 
                    context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                    "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:ApiEndpoints:PermitLimit", 100),
                Window = TimeSpan.Parse(builder.Configuration["RateLimiting:ApiEndpoints:Window"] ?? "00:01:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queueing
            });
    });

    // Password reset rate limiting: 3 requests per hour per email
    options.AddPolicy("passwordreset", context =>
    {
        var userId = context.User?.Identity?.Name ?? 
                    context.Connection.RemoteIpAddress?.ToString() ?? 
                    context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                    "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PasswordResetEndpoint:PermitLimit", 3),
                Window = TimeSpan.Parse(builder.Configuration["RateLimiting:PasswordResetEndpoint:Window"] ?? "01:00:00"),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queueing
            });
    });

    // Configure rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        // Add Retry-After header
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            statusCode = 429
        }, cancellationToken: cancellationToken);
    };
});

// Configure forwarded headers for proxy/load balancer support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Clear known networks and proxies to accept headers from any proxy
    // In production, you should configure specific known proxies for security
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add session support with secure cookie configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Requires HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // Prevent CSRF attacks
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure forwarded headers middleware (must be before other middleware)
app.UseForwardedHeaders();

// Enable host filtering middleware
app.UseHostFiltering();

// Add security headers middleware before static files and routing
app.UseSecurityHeaders();

app.UseStaticFiles();
app.UseRouting();

// Add rate limiting middleware after routing
app.UseRateLimiter();

app.UseSession();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Migrations are opt-in outside development to avoid schema changes on every production start.
    var migrateOnStartup = app.Environment.IsDevelopment() ||
        builder.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup");

    if (migrateOnStartup)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed during startup. Continuing without auto-migration.");
        }
    }
    else
    {
        logger.LogInformation("Startup migrations are disabled. Set Database:AutoMigrateOnStartup=true to enable.");
    }

    // Super admin bootstrap is disabled by default and requires explicit credentials from configuration.
    var bootstrapSuperAdmin = builder.Configuration.GetValue<bool>("BootstrapSuperAdmin:Enabled");
    var bootstrapEmail = builder.Configuration["BootstrapSuperAdmin:Email"];
    var bootstrapPassword = builder.Configuration["BootstrapSuperAdmin:Password"];

    if (bootstrapSuperAdmin)
    {
        if (string.IsNullOrWhiteSpace(bootstrapEmail) || string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            logger.LogWarning("BootstrapSuperAdmin is enabled but Email/Password is missing. Skipping bootstrap.");
        }
        else if (!ValidateBootstrapPassword(bootstrapPassword, out string validationError))
        {
            logger.LogError("BootstrapSuperAdmin password validation failed: {Error}. Skipping bootstrap for security reasons.", validationError);
        }
        else if (!await dbContext.Users.AnyAsync(u => u.Email == bootstrapEmail))
        {
            dbContext.Users.Add(new User
            {
                FirstName = "Super",
                LastName = "Administrator",
                Email = bootstrapEmail,
                Password = PasswordHasher.HashPassword(bootstrapPassword),
                Role = RoleNames.SuperAdmin,
                Status = "Active"
            });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Bootstrap super admin account created.");
        }
    }
}

// Password validation helper for bootstrap
static bool ValidateBootstrapPassword(string password, out string errorMessage)
{
    errorMessage = string.Empty;

    // Blacklist of known weak passwords
    var weakPasswordBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Admin@123",
        "Password123!",
        "Welcome123!",
        "Passw0rd!",
        "P@ssw0rd",
        "Admin123!",
        "Password1!",
        "Qwerty123!",
        "Letmein123!",
        "Welcome1!"
    };

    // Check minimum length (12 characters)
    if (password.Length < 12)
    {
        errorMessage = "Password must be at least 12 characters long.";
        return false;
    }

    // Check for at least one uppercase letter
    if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
    {
        errorMessage = "Password must contain at least one uppercase letter (A-Z).";
        return false;
    }

    // Check for at least one lowercase letter
    if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]"))
    {
        errorMessage = "Password must contain at least one lowercase letter (a-z).";
        return false;
    }

    // Check for at least one digit
    if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
    {
        errorMessage = "Password must contain at least one digit (0-9).";
        return false;
    }

    // Check for at least one special character
    if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*()\-_=+\[\]{}|;:,.<>?]"))
    {
        errorMessage = "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?).";
        return false;
    }

    // Check against blacklist of known weak passwords
    if (weakPasswordBlacklist.Contains(password))
    {
        errorMessage = "This password is known to be weak and commonly used. Please choose a different password.";
        return false;
    }

    return true;
}
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Lightweight keep-alive endpoint for uptime monitors (no DB access)
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapRazorPages();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
