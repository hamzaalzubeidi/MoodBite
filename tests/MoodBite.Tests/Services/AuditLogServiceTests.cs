using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MoodBite.Constants;
using MoodBite.Services;
using MoodBite.Tests.TestSupport;

namespace MoodBite.Tests.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_records_actor_target_and_safe_metadata()
    {
        await using var db = TestDb.CreateDb();
        var context = TestDb.HttpContextFor("auditor", ApplicationRoles.Admin);
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Request.Headers.UserAgent = "test-agent";

        var service = new AuditLogService(
            db,
            new HttpContextAccessor { HttpContext = context },
            NullLogger<AuditLogService>.Instance);

        await service.LogAsync(
            "admin.test.action",
            "ApplicationUser",
            "target-1",
            clinicId: 7,
            targetUserId: "patient-1",
            summary: "Test audit entry.",
            metadata: new { role = ApplicationRoles.User, count = 2 });

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal("auditor", log.ActorUserId);
        Assert.Equal("auditor@example.test", log.ActorEmail);
        Assert.Equal(ApplicationRoles.Admin, log.ActorRoles);
        Assert.Equal(7, log.ClinicId);
        Assert.Equal("patient-1", log.TargetUserId);
        Assert.Contains("role", log.MetadataJson);
        Assert.Contains("count", log.MetadataJson);
    }

    [Fact]
    public async Task LogAsync_does_not_store_sensitive_metadata_keys_or_values()
    {
        await using var db = TestDb.CreateDb();
        var service = new AuditLogService(
            db,
            new HttpContextAccessor { HttpContext = TestDb.HttpContextFor("owner", ApplicationRoles.ClinicOwner) },
            NullLogger<AuditLogService>.Instance);

        await service.LogAsync(
            "clinic.test.sensitive",
            "ClinicalNote",
            "42",
            clinicId: 1,
            metadata: new
            {
                password = "Secret123",
                resetLink = "https://example.test/reset?token=abc",
                noteContent = "private medical note content",
                safeStatus = "active",
                description = "contains token=abc"
            });

        var json = Assert.Single(db.AuditLogs).MetadataJson;
        Assert.DoesNotContain("Secret123", json);
        Assert.DoesNotContain("reset", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private medical", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=abc", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("safeStatus", json);
        Assert.Contains("[redacted]", json);
    }
}
