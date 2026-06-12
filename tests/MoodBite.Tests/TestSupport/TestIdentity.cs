using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;

namespace MoodBite.Tests.TestSupport;

internal static class TestIdentity
{
    public static ServiceProvider CreateProvider(string? dbName = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString("N")));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<TranslationService>();
        services.AddScoped<MealPlanService>();
        return services.BuildServiceProvider();
    }

    public static GeminiService CreateGeminiService(
        IServiceProvider provider,
        string apiKey = "")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = apiKey
            })
            .Build();

        return new GeminiService(
            new HttpClient(new OfflineHttpMessageHandler()),
            config,
            provider.GetRequiredService<ILogger<GeminiService>>(),
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IHttpContextAccessor>());
    }

    public static async Task<ApplicationUser> EnsureUserAsync(
        ServiceProvider provider,
        string id = "test-user",
        string email = "test-user@example.test")
    {
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id);
        if (user != null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            FullName = "Test User",
            EmailConfirmed = true,
            IsActive = true,
            PreferredLanguage = "en"
        };

        var result = await userManager.CreateAsync(user, "Test@123456");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }
}

internal sealed class OfflineHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("{}")
        });
}

internal sealed class TestEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "MoodBite.Tests";
    public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
