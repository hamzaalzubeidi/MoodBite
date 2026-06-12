using MoodBite.Services;
using MoodBite.Tests.TestSupport;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Tests.Services;

public class ClinicNotesServiceTests
{
    [Fact]
    public async Task Create_note_persists_author_patient_and_content()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        var note = await service.CreateNoteAsync(fx.Clinic.Id, fx.Patient.Id, new ClinicNoteInputViewModel
        {
            NoteType = "nutrition",
            Title = "Initial",
            Content = "Patient needs more protein.",
            IsPrivate = true
        });

        Assert.NotNull(note);
        Assert.Equal(fx.Dietitian.Id, note!.AuthorId);
        Assert.Equal(fx.Patient.Id, note.PatientId);
        Assert.False(note.IsSharedWithPatient);
    }

    [Fact]
    public async Task Edit_note_updates_content_and_privacy()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);
        var note = await service.CreateNoteAsync(fx.Clinic.Id, fx.Patient.Id, new ClinicNoteInputViewModel { Content = "Old" });

        await service.UpdateNoteAsync(note!, new ClinicNoteInputViewModel
        {
            NoteType = "progress",
            Title = "Updated",
            Content = "New content",
            IsPrivate = false
        });

        Assert.Equal("Updated", note!.Title);
        Assert.Equal("New content", note.Content);
        Assert.True(note.IsSharedWithPatient);
        Assert.NotNull(note.UpdatedAt);
    }

    [Fact]
    public async Task Archive_note_hides_it_from_default_queries()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);
        var note = await service.CreateNoteAsync(fx.Clinic.Id, fx.Patient.Id, new ClinicNoteInputViewModel { Content = "Archive me" });

        await service.ArchiveNoteAsync(note!);

        Assert.Null(await service.GetPatientNoteAsync(fx.Clinic.Id, fx.Patient.Id, note!.Id));
        Assert.NotNull(await service.GetPatientNoteAsync(fx.Clinic.Id, fx.Patient.Id, note.Id, includeArchived: true));
    }

    [Fact]
    public async Task Cross_clinic_access_returns_null()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);
        var note = await service.CreateNoteAsync(fx.Clinic.Id, fx.Patient.Id, new ClinicNoteInputViewModel { Content = "Clinic one" });

        var wrongClinicResult = await service.GetPatientNoteAsync(fx.OtherClinic.Id, fx.Patient.Id, note!.Id);

        Assert.Null(wrongClinicResult);
    }

    [Fact]
    public async Task Missing_note_returns_null()
    {
        var fx = await TestDb.SeedClinicFixtureAsync();
        var service = CreateService(fx, fx.Dietitian.Id);

        Assert.Null(await service.GetPatientNoteAsync(fx.Clinic.Id, fx.Patient.Id, 999));
    }

    private static ClinicNotesService CreateService(ClinicFixture fx, string userId) =>
        new(fx.Db, TestDb.CurrentUser(fx.Db, userId));
}
