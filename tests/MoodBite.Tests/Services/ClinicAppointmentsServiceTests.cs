using MoodBite.Services;
using MoodBite.Tests.TestSupport;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Tests.Services;

public class ClinicAppointmentsServiceTests
{
    [Fact]
    public async Task Create_appointment_persists_current_user_as_dietitian()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        var appointment = await service.CreateAppointmentAsync(fx.Clinic.Id, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(2)));

        Assert.NotNull(appointment);
        Assert.Equal(fx.Dietitian.Id, appointment!.DietitianId);
        Assert.Equal("scheduled", appointment.Status);
    }

    [Fact]
    public async Task Edit_appointment_updates_status_and_cancelled_at()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);
        var appointment = await service.CreateAppointmentAsync(fx.Clinic.Id, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(2)));

        await service.UpdateAppointmentAsync(appointment!, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(3), "cancelled"));

        Assert.Equal("cancelled", appointment!.Status);
        Assert.NotNull(appointment.CancelledAt);
    }

    [Fact]
    public async Task Filters_return_upcoming_completed_and_cancelled_sets()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);
        await service.CreateAppointmentAsync(fx.Clinic.Id, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(2)));
        await service.CreateAppointmentAsync(fx.Clinic.Id, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(-2), "completed"));
        await service.CreateAppointmentAsync(fx.Clinic.Id, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(4), "cancelled"));

        var upcoming = await service.BuildIndexModelAsync(fx.Clinic.Id, fx.Clinic.Name, "upcoming", null);
        var completed = await service.BuildIndexModelAsync(fx.Clinic.Id, fx.Clinic.Name, "completed", null);
        var cancelled = await service.BuildIndexModelAsync(fx.Clinic.Id, fx.Clinic.Name, "cancelled", null);

        Assert.Single(upcoming.Appointments);
        Assert.Single(completed.Appointments);
        Assert.Single(cancelled.Appointments);
    }

    [Fact]
    public async Task Cross_clinic_appointment_access_returns_null()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);
        var appointment = await service.CreateAppointmentAsync(fx.Clinic.Id, Input(fx.Patient.Id, DateTime.UtcNow.AddDays(2)));

        var wrongClinicResult = await service.GetAppointmentAsync(appointment!.Id, fx.OtherClinic.Id);

        Assert.Null(wrongClinicResult);
    }

    [Fact]
    public async Task Missing_appointment_returns_null()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        Assert.Null(await service.GetAppointmentAsync(999, fx.Clinic.Id));
    }

    private static ClinicAppointmentsService CreateService(ClinicFixture fx, string userId) =>
        new(fx.Db, TestDb.CurrentUser(fx.Db, userId));

    private static ClinicAppointmentInputViewModel Input(string patientId, DateTime startsAt, string status = "scheduled") =>
        new()
        {
            PatientId = patientId,
            StartsAt = startsAt,
            Status = status,
            DurationMinutes = 30,
            VisitType = "followUp",
            Location = "Room 1",
            Notes = "Demo appointment"
        };
}
