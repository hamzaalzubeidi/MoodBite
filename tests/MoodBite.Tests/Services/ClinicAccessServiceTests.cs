using MoodBite.Constants;
using MoodBite.Services;
using MoodBite.Tests.TestSupport;

namespace MoodBite.Tests.Services;

public class ClinicAccessServiceTests
{
    [Fact]
    public async Task Admin_access_is_allowed_by_platform_admin_check()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = new ClinicAccessService(fx.Db, TestDb.CurrentUser(fx.Db, fx.Admin.Id));

        Assert.True(await service.IsPlatformAdminAsync(fx.Admin.Id));
        Assert.True(await service.CanManageClinicAsync(fx.Admin.Id, fx.Clinic.Id));
    }

    [Fact]
    public async Task Clinic_owner_can_manage_own_clinic()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = new ClinicAccessService(fx.Db, TestDb.CurrentUser(fx.Db, fx.Owner.Id));

        Assert.True(await service.IsClinicOwnerAsync(fx.Owner.Id, fx.Clinic.Id));
        Assert.True(await service.CanManageClinicAsync(fx.Owner.Id, fx.Clinic.Id));
    }

    [Fact]
    public async Task Dietitian_and_staff_can_access_but_not_manage_clinic()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = new ClinicAccessService(fx.Db, TestDb.CurrentUser(fx.Db, fx.Dietitian.Id));

        Assert.True(await service.IsDietitianAsync(fx.Dietitian.Id, fx.Clinic.Id));
        Assert.True(await service.IsClinicStaffAsync(fx.Staff.Id, fx.Clinic.Id));
        Assert.True(await service.IsClinicMemberAsync(fx.Dietitian.Id, fx.Clinic.Id));
        Assert.True(await service.IsClinicMemberAsync(fx.Staff.Id, fx.Clinic.Id));
        Assert.False(await service.CanManageClinicAsync(fx.Dietitian.Id, fx.Clinic.Id));
        Assert.False(await service.CanManageClinicAsync(fx.Staff.Id, fx.Clinic.Id));
    }

    [Fact]
    public async Task Unauthorized_user_is_blocked()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = new ClinicAccessService(fx.Db, TestDb.CurrentUser(fx.Db, fx.Outsider.Id));

        Assert.False(await service.IsClinicMemberAsync(fx.Outsider.Id, fx.Clinic.Id));
        Assert.False(await service.CanManageClinicAsync(fx.Outsider.Id, fx.Clinic.Id));
        Assert.False(await service.CanAccessPatientAsync(fx.Outsider.Id, fx.Clinic.Id, fx.Patient.Id));
    }

    [Fact]
    public async Task User_cannot_access_unrelated_clinic()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = new ClinicAccessService(fx.Db, TestDb.CurrentUser(fx.Db, fx.Owner.Id));

        Assert.False(await service.IsClinicMemberAsync(fx.Owner.Id, fx.OtherClinic.Id));
        Assert.False(await service.PatientBelongsToClinicAsync(fx.Patient.Id, fx.OtherClinic.Id));
    }

    [Fact]
    public async Task Inactive_clinic_membership_is_not_active_access()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var member = fx.Db.ClinicMembers.First(m => m.UserId == fx.Staff.Id && m.ClinicId == fx.Clinic.Id);
        member.IsActive = false;
        await fx.Db.SaveChangesAsync();
        var service = new ClinicAccessService(fx.Db, TestDb.CurrentUser(fx.Db, fx.Staff.Id));

        Assert.False(await service.IsClinicMemberAsync(fx.Staff.Id, fx.Clinic.Id));
        Assert.True(await service.IsClinicMemberAsync(fx.Staff.Id, fx.Clinic.Id, activeOnly: false));
    }
}
