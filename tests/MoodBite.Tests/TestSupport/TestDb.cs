using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;

namespace MoodBite.Tests.TestSupport;

internal static class TestDb
{
    public static ApplicationDbContext CreateDb(string? name = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    public static DefaultHttpContext HttpContextFor(string? userId, params string[] claimsRoles)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new TestSessionFeature(new TestSession()));
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, $"{userId}@example.test")
            };

            claims.AddRange(claimsRoles.Select(role => new Claim(ClaimTypes.Role, role)));
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        return context;
    }

    public static CurrentUserService CurrentUser(ApplicationDbContext db, string? userId, params string[] claimsRoles)
    {
        var accessor = new HttpContextAccessor { HttpContext = HttpContextFor(userId, claimsRoles) };
        return new CurrentUserService(accessor, db);
    }

    public static TranslationService Translation(string lang = "en")
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new TestSessionFeature(new Dictionary<string, string> { ["lang"] = lang }));
        context.Request.Headers.Cookie = $"lang={lang}";
        return new TranslationService(new HttpContextAccessor { HttpContext = context });
    }

    public static ApplicationUser User(string id, string email, string fullName) =>
        new()
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = fullName,
            IsActive = true,
            EmailConfirmed = true
        };

    public static IdentityRole Role(string name) =>
        new(name)
        {
            Id = $"role-{name}",
            NormalizedName = name.ToUpperInvariant()
        };

    public static void AddRole(ApplicationDbContext db, ApplicationUser user, string role)
    {
        if (!db.Roles.Any(r => r.Name == role))
        {
            db.Roles.Add(Role(role));
        }

        var roleId = db.Roles.Local.FirstOrDefault(r => r.Name == role)?.Id
                     ?? db.Roles.First(r => r.Name == role).Id;

        if (!db.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == roleId))
        {
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = roleId });
        }
    }

    public static async Task<ClinicFixture> SeedClinicFixtureAsync()
    {
        var db = CreateDb();
        var admin = User("admin", "admin@example.test", "Admin");
        var owner = User("owner", "owner@example.test", "Owner");
        var dietitian = User("dietitian", "dietitian@example.test", "Dietitian");
        var staff = User("staff", "staff@example.test", "Staff");
        var outsider = User("outsider", "outsider@example.test", "Outsider");
        var patient = User("patient", "patient@example.test", "Patient");
        var otherPatient = User("other-patient", "other@example.test", "Other Patient");

        db.Users.AddRange(admin, owner, dietitian, staff, outsider, patient, otherPatient);
        AddRole(db, admin, ApplicationRoles.Admin);
        AddRole(db, owner, ApplicationRoles.ClinicOwner);
        AddRole(db, dietitian, ApplicationRoles.Dietitian);
        AddRole(db, staff, ApplicationRoles.ClinicStaff);
        AddRole(db, outsider, ApplicationRoles.User);

        var clinic = new Clinic { Id = 1, Name = "Clinic One", Slug = "clinic-one", IsActive = true };
        var otherClinic = new Clinic { Id = 2, Name = "Clinic Two", Slug = "clinic-two", IsActive = true };
        db.Clinics.AddRange(clinic, otherClinic);
        db.ClinicMembers.AddRange(
            new ClinicMember { Id = 1, ClinicId = clinic.Id, UserId = owner.Id, Role = ApplicationRoles.ClinicOwner, IsActive = true },
            new ClinicMember { Id = 2, ClinicId = clinic.Id, UserId = dietitian.Id, Role = ApplicationRoles.Dietitian, IsActive = true },
            new ClinicMember { Id = 3, ClinicId = clinic.Id, UserId = staff.Id, Role = ApplicationRoles.ClinicStaff, IsActive = true },
            new ClinicMember { Id = 4, ClinicId = otherClinic.Id, UserId = outsider.Id, Role = ApplicationRoles.Dietitian, IsActive = true });

        db.ClinicPatients.AddRange(
            new ClinicPatient
            {
                Id = 1,
                ClinicId = clinic.Id,
                PatientId = patient.Id,
                PrimaryDietitianId = dietitian.Id,
                Status = "active",
                ConsentGranted = true
            },
            new ClinicPatient
            {
                Id = 2,
                ClinicId = otherClinic.Id,
                PatientId = otherPatient.Id,
                Status = "active",
                ConsentGranted = true
            });

        await db.SaveChangesAsync();
        return new ClinicFixture(db, admin, owner, dietitian, staff, outsider, patient, otherPatient, clinic, otherClinic);
    }
}

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new();

    public TestSession()
    {
    }

    public TestSession(IDictionary<string, string> values)
    {
        foreach (var pair in values)
        {
            _values[pair.Key] = System.Text.Encoding.UTF8.GetBytes(pair.Value);
        }
    }

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public IEnumerable<string> Keys => _values.Keys;

    public void Clear() => _values.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _values.Remove(key);

    public void Set(string key, byte[] value) => _values[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
}

internal sealed class TestSessionFeature : ISessionFeature
{
    public TestSessionFeature(ISession session)
    {
        Session = session;
    }

    public TestSessionFeature(IDictionary<string, string> values)
    {
        Session = new TestSession(values);
    }

    public ISession Session { get; set; }
}

internal sealed record ClinicFixture(
    ApplicationDbContext Db,
    ApplicationUser Admin,
    ApplicationUser Owner,
    ApplicationUser Dietitian,
    ApplicationUser Staff,
    ApplicationUser Outsider,
    ApplicationUser Patient,
    ApplicationUser OtherPatient,
    Clinic Clinic,
    Clinic OtherClinic);
