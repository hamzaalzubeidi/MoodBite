using MoodBite.Services;
using MoodBite.Tests.TestSupport;

namespace MoodBite.Tests.Services;

public class ClinicPatientAccessContextServiceTests
{
    [Fact]
    public async Task Valid_clinic_patient_link_resolves_context()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        var access = await service.ResolvePatientAccessAsync(fx.Patient.Id, fx.Clinic.Id);

        Assert.NotNull(access);
        Assert.Equal(fx.Clinic.Id, access!.ClinicId);
        Assert.Equal(fx.Patient.Id, access.PatientId);
    }

    [Fact]
    public async Task Missing_patient_link_fails_safely()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        var access = await service.ResolvePatientAccessAsync("missing", fx.Clinic.Id);

        Assert.Null(access);
    }

    [Fact]
    public async Task Wrong_clinic_fails_safely()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        var access = await service.ResolvePatientAccessAsync(fx.Patient.Id, fx.OtherClinic.Id);

        Assert.Null(access);
    }

    [Fact]
    public async Task Inactive_patient_link_still_resolves_for_clinical_history()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var link = fx.Db.ClinicPatients.First(p => p.PatientId == fx.Patient.Id && p.ClinicId == fx.Clinic.Id);
        link.Status = "archived";
        await fx.Db.SaveChangesAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        var access = await service.ResolvePatientAccessAsync(fx.Patient.Id, fx.Clinic.Id);

        Assert.NotNull(access);
    }

    [Fact]
    public async Task Unauthenticated_user_fails_safely()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, null);

        var access = await service.ResolvePatientAccessAsync(fx.Patient.Id, fx.Clinic.Id);

        Assert.Null(access);
    }

    private static ClinicPatientAccessContextService CreateService(ClinicFixture fx, string? userId)
    {
        var currentUser = TestDb.CurrentUser(fx.Db, userId);
        var clinicAccess = new ClinicAccessService(fx.Db, currentUser);
        return new ClinicPatientAccessContextService(fx.Db, currentUser, clinicAccess);
    }
}
