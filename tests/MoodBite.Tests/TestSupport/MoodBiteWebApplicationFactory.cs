using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;

namespace MoodBite.Tests.TestSupport;

public sealed class MoodBiteWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"MoodBiteRouteTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=MoodBiteRouteTests;Trusted_Connection=True;MultipleActiveResultSets=true",
                ["Gemini:ApiKey"] = "",
                ["Gemini:HttpTimeoutSeconds"] = "2",
                ["OpenFoodFacts:BaseUrl"] = "https://example.test/",
                ["OpenFoodFacts:UserAgent"] = "MoodBite.Tests/1.0",
                ["OpenFoodFacts:TimeoutSeconds"] = "2",
                ["MoodBite:SeedDemoData"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<IDatabaseProvider>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_dbName));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    public HttpClient CreateUnauthenticatedClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string userId, params string[] roles)
    {
        await SeedUsersAsync();
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        return client;
    }

    private async Task SeedUsersAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in ApplicationRoles.SeededRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "patient-route", ApplicationRoles.User);
        await EnsureUserAsync(userManager, "admin-route", ApplicationRoles.Admin);
        await EnsureUserAsync(userManager, "owner-route", ApplicationRoles.ClinicOwner);
        await EnsureUserAsync(userManager, "dietitian-route", ApplicationRoles.Dietitian);
        await EnsureUserAsync(userManager, "staff-route", ApplicationRoles.ClinicStaff);

        if (!await db.Clinics.AnyAsync(c => c.Slug == "route-test-clinic"))
        {
            var clinic = new Clinic { Name = "Route Test Clinic", Slug = "route-test-clinic", IsActive = true };
            db.Clinics.Add(clinic);
            await db.SaveChangesAsync();

            db.ClinicMembers.AddRange(
                new ClinicMember { ClinicId = clinic.Id, UserId = "owner-route", Role = ApplicationRoles.ClinicOwner, IsActive = true },
                new ClinicMember { ClinicId = clinic.Id, UserId = "dietitian-route", Role = ApplicationRoles.Dietitian, IsActive = true },
                new ClinicMember { ClinicId = clinic.Id, UserId = "staff-route", Role = ApplicationRoles.ClinicStaff, IsActive = true });

            db.ClinicPatients.Add(new ClinicPatient
            {
                ClinicId = clinic.Id,
                PatientId = "patient-route",
                PrimaryDietitianId = "dietitian-route",
                Status = "active",
                ConsentGranted = true,
                ConsentGrantedAt = DateTime.UtcNow
            });

            db.HealthProfiles.Add(new HealthProfile
            {
                UserId = "patient-route",
                Age = 31,
                Gender = "female",
                Height = 168,
                Weight = 72,
                Goal = "loseWeight",
                ActivityLevel = "moderate",
                CookingStyle = "moderate",
                Budget = "medium",
                DietSlug = "mediterranean",
                CalorieTarget = 1700,
                WaterGoal = 8
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string id, string role)
    {
        var email = $"{id}@example.test";
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FullName = id,
                IsActive = true,
                EmailConfirmed = true,
                PreferredLanguage = "en"
            };

            var result = await userManager.CreateAsync(user, "Test@123456");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
