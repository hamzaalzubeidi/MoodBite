using Microsoft.AspNetCore.Identity;
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

// Custom services
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<MealPlanService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AchievementService>();

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

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

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
