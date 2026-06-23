using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;

// Load developer-local variables before ASP.NET Core builds its configuration.
// Real deployments should set environment variables in the hosting platform instead of using .env.
LoadDotEnvFile(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

// Environment variables override appsettings.json. This explicit provider also picks up .env values.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddInMemoryCollection(BuildEnvironmentVariableAliases());

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        "Database connection string is missing. Set MOODBITE_DB_CONNECTION_STRING or ConnectionStrings__DefaultConnection.");
}

var geminiTimeout = GetRequiredPositiveSeconds(
    builder.Configuration,
    "Gemini:HttpTimeoutSeconds",
    "GEMINI_HTTP_TIMEOUT_SECONDS");

var openFoodFactsBaseUrl = GetRequiredConfigurationValue(
    builder.Configuration,
    "OpenFoodFacts:BaseUrl",
    "OPENFOODFACTS_BASE_URL");
var openFoodFactsUserAgent = GetRequiredConfigurationValue(
    builder.Configuration,
    "OpenFoodFacts:UserAgent",
    "OPENFOODFACTS_USER_AGENT");
var openFoodFactsTimeout = GetRequiredPositiveSeconds(
    builder.Configuration,
    "OpenFoodFacts:TimeoutSeconds",
    "OPENFOODFACTS_TIMEOUT_SECONDS");

if (!Uri.TryCreate(openFoodFactsBaseUrl, UriKind.Absolute, out var openFoodFactsBaseUri))
{
    throw new InvalidOperationException("OPENFOODFACTS_BASE_URL must be an absolute URL.");
}

// Add services
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// EF Core + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(defaultConnection));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// In-memory cache (used by GeminiService rate limiter)
builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    AddFixedWindowPolicy(options, "auth", builder.Configuration, "RateLimiting:Auth", 10, TimeSpan.FromMinutes(1));
    AddFixedWindowPolicy(options, "ai", builder.Configuration, "RateLimiting:Ai", 20, TimeSpan.FromMinutes(1));
    AddFixedWindowPolicy(options, "scanner", builder.Configuration, "RateLimiting:Scanner", 30, TimeSpan.FromMinutes(1));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// Custom services
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<MealPlanService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<ClinicAccessService>();
builder.Services.AddScoped<PatientContextService>();
builder.Services.AddScoped<ClinicPatientAccessContextService>();
builder.Services.AddScoped<ClinicNotesService>();
builder.Services.AddScoped<ClinicAppointmentsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// HttpClient — typed client for GeminiService (also registers GeminiService itself)
builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.Timeout = geminiTimeout;
});
// Named/unnamed factory for controllers that use IHttpClientFactory directly
builder.Services.AddHttpClient("openfoodfacts", client =>
{
    client.BaseAddress = openFoodFactsBaseUri;
    client.DefaultRequestHeaders.Add("User-Agent", openFoodFactsUserAgent);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = openFoodFactsTimeout;
});

var app = builder.Build();

ProductionConfigurationValidator.Validate(app);

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seedDemoData = app.Environment.IsDevelopment() &&
                           app.Configuration.GetValue<bool>("MoodBite:SeedDemoData");
        await DbSeeder.SeedAsync(services, seedDemoData);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponseAsync
});

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "clinic",
    pattern: "Clinic/{controller=ClinicDashboard}/{action=Index}/{id?}",
    defaults: new { area = "Clinic" });

app.Run();


static void AddFixedWindowPolicy(
    RateLimiterOptions options,
    string policyName,
    IConfiguration configuration,
    string sectionName,
    int defaultPermitLimit,
    TimeSpan defaultWindow)
{
    var permitLimit = GetOptionalPositiveInt(configuration, $"{sectionName}:PermitLimit", defaultPermitLimit);
    var windowSeconds = GetOptionalPositiveInt(configuration, $"{sectionName}:WindowSeconds", (int)defaultWindow.TotalSeconds);
    var window = TimeSpan.FromSeconds(windowSeconds);

    options.AddPolicy(policyName, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
}

static string GetRateLimitPartitionKey(HttpContext context)
{
    var userId = context.User?.Identity?.IsAuthenticated == true
        ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        : null;

    if (!string.IsNullOrWhiteSpace(userId))
    {
        return $"user:{userId}";
    }

    var ip = context.Connection.RemoteIpAddress?.ToString();
    return string.IsNullOrWhiteSpace(ip) ? "anonymous" : $"ip:{ip}";
}

static int GetOptionalPositiveInt(IConfiguration configuration, string key, int fallback) =>
    int.TryParse(configuration[key], out var value) && value > 0 ? value : fallback;

static async Task WriteHealthResponseAsync(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
}
static Dictionary<string, string?> BuildEnvironmentVariableAliases()
{
    var aliases = new Dictionary<string, string?>();

    AddAlias(aliases, "GEMINI_API_KEY", "Gemini:ApiKey");
    AddAlias(aliases, "MOODBITE_DB_CONNECTION_STRING", "ConnectionStrings:DefaultConnection");
    AddAlias(aliases, "ALLOWED_HOSTS", "AllowedHosts");
    AddAlias(aliases, "GEMINI_HTTP_TIMEOUT_SECONDS", "Gemini:HttpTimeoutSeconds");
    AddAlias(aliases, "OPENFOODFACTS_BASE_URL", "OpenFoodFacts:BaseUrl");
    AddAlias(aliases, "OPENFOODFACTS_USER_AGENT", "OpenFoodFacts:UserAgent");
    AddAlias(aliases, "OPENFOODFACTS_TIMEOUT_SECONDS", "OpenFoodFacts:TimeoutSeconds");
    AddAlias(aliases, "MOODBITE_SEED_DEMO_DATA", "MoodBite:SeedDemoData");
    AddAlias(aliases, "EMAIL_PROVIDER", "Email:Provider");
    AddAlias(aliases, "EMAIL_FROM", "Email:FromEmail");
    AddAlias(aliases, "EMAIL_FROM_NAME", "Email:FromName");
    AddAlias(aliases, "SMTP_HOST", "Email:Smtp:Host");
    AddAlias(aliases, "SMTP_PORT", "Email:Smtp:Port");
    AddAlias(aliases, "SMTP_USERNAME", "Email:Smtp:Username");
    AddAlias(aliases, "SMTP_PASSWORD", "Email:Smtp:Password");
    AddAlias(aliases, "SMTP_ENABLE_SSL", "Email:Smtp:EnableSsl");
    AddAlias(aliases, "RATE_LIMIT_AUTH_PERMIT_LIMIT", "RateLimiting:Auth:PermitLimit");
    AddAlias(aliases, "RATE_LIMIT_AUTH_WINDOW_SECONDS", "RateLimiting:Auth:WindowSeconds");
    AddAlias(aliases, "RATE_LIMIT_AI_PERMIT_LIMIT", "RateLimiting:Ai:PermitLimit");
    AddAlias(aliases, "RATE_LIMIT_AI_WINDOW_SECONDS", "RateLimiting:Ai:WindowSeconds");
    AddAlias(aliases, "RATE_LIMIT_SCANNER_PERMIT_LIMIT", "RateLimiting:Scanner:PermitLimit");
    AddAlias(aliases, "RATE_LIMIT_SCANNER_WINDOW_SECONDS", "RateLimiting:Scanner:WindowSeconds");
    AddAlias(aliases, "LOG_LEVEL_DEFAULT", "Logging:LogLevel:Default");
    AddAlias(aliases, "LOG_LEVEL_ASPNETCORE", "Logging:LogLevel:Microsoft.AspNetCore");

    return aliases;
}

static void AddAlias(IDictionary<string, string?> aliases, string environmentVariable, string configurationKey)
{
    var value = Environment.GetEnvironmentVariable(environmentVariable);
    if (!string.IsNullOrWhiteSpace(value))
    {
        aliases[configurationKey] = value;
    }
}

static string GetRequiredConfigurationValue(
    IConfiguration configuration,
    string configurationKey,
    string environmentVariable)
{
    var value = configuration[configurationKey];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Configuration value '{configurationKey}' is missing. Set {environmentVariable}.");
    }

    return value;
}

static TimeSpan GetRequiredPositiveSeconds(
    IConfiguration configuration,
    string configurationKey,
    string environmentVariable)
{
    var rawValue = GetRequiredConfigurationValue(configuration, configurationKey, environmentVariable);
    if (!int.TryParse(rawValue, out var seconds) || seconds <= 0)
    {
        throw new InvalidOperationException($"{environmentVariable} must be a positive whole number of seconds.");
    }

    return TimeSpan.FromSeconds(seconds);
}

static void LoadDotEnvFile(string path)
{
    if (!File.Exists(path))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
        {
            continue;
        }

        if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
        {
            line = line["export ".Length..].TrimStart();
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();
        if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
        {
            continue;
        }

        Environment.SetEnvironmentVariable(key, UnquoteDotEnvValue(value));
    }
}

static string UnquoteDotEnvValue(string value)
{
    if (value.Length < 2)
    {
        return value;
    }

    var quote = value[0];
    if ((quote != '"' && quote != '\'') || value[^1] != quote)
    {
        return value;
    }

    var innerValue = value[1..^1];
    return quote == '"'
        ? innerValue
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
        : innerValue;
}

public partial class Program { }
