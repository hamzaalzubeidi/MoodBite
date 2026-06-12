using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Services
{
    public class ClinicNotesService
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;

        public ClinicNotesService(ApplicationDbContext db, CurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<List<ClinicNoteListItemViewModel>> GetPatientNotesAsync(
            int clinicId,
            string patientId,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var notes = await _db.ClinicalNotes.AsNoTracking()
                .Where(n => n.ClinicId == clinicId && n.PatientId == patientId && !n.IsArchived)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Content,
                    AuthorName = n.Author.FullName,
                    n.CreatedAt,
                    n.IsSharedWithPatient
                })
                .ToListAsync(cancellationToken);

            return notes.Select(n => new ClinicNoteListItemViewModel
                {
                    Id = n.Id,
                    Title = string.IsNullOrWhiteSpace(n.Title) ? "Clinical note" : n.Title,
                    Preview = BuildPreview(n.Content),
                    AuthorName = n.AuthorName,
                    CreatedAt = n.CreatedAt,
                    IsPrivate = !n.IsSharedWithPatient
                })
                .ToList();
        }

        public async Task<ClinicalNote?> GetPatientNoteAsync(
            int clinicId,
            string patientId,
            int noteId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            var query = _db.ClinicalNotes
                .Include(n => n.Author)
                .Where(n => n.Id == noteId && n.ClinicId == clinicId && n.PatientId == patientId);

            if (!includeArchived)
            {
                query = query.Where(n => !n.IsArchived);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ClinicalNote?> CreateNoteAsync(
            int clinicId,
            string patientId,
            ClinicNoteInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var authorId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(authorId))
            {
                return null;
            }

            var note = new ClinicalNote
            {
                ClinicId = clinicId,
                PatientId = patientId,
                AuthorId = authorId,
                NoteType = NormalizeNoteType(input.NoteType),
                Title = NormalizeTitle(input.Title),
                Content = NormalizeContent(input.Content),
                IsSharedWithPatient = !input.IsPrivate,
                CreatedAt = DateTime.UtcNow
            };

            _db.ClinicalNotes.Add(note);
            await _db.SaveChangesAsync(cancellationToken);
            return note;
        }

        public async Task<bool> UpdateNoteAsync(
            ClinicalNote note,
            ClinicNoteInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            note.NoteType = NormalizeNoteType(input.NoteType);
            note.Title = NormalizeTitle(input.Title);
            note.Content = NormalizeContent(input.Content);
            note.IsSharedWithPatient = !input.IsPrivate;
            note.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ArchiveNoteAsync(ClinicalNote note, CancellationToken cancellationToken = default)
        {
            note.IsArchived = true;
            note.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public ClinicNoteDetailsViewModel BuildDetailsModel(
            ClinicPatientAccessContext access,
            ClinicPatientCompactViewModel patient,
            ClinicalNote note)
        {
            return new ClinicNoteDetailsViewModel
            {
                ClinicId = access.ClinicId,
                ClinicName = access.ClinicName,
                Patient = patient,
                Note = new ClinicNoteListItemViewModel
                {
                    Id = note.Id,
                    Title = string.IsNullOrWhiteSpace(note.Title) ? "Clinical note" : note.Title,
                    Preview = BuildPreview(note.Content),
                    AuthorName = note.Author.FullName,
                    CreatedAt = note.CreatedAt,
                    IsPrivate = !note.IsSharedWithPatient
                },
                Content = note.Content,
                NoteType = note.NoteType,
                IsPrivate = !note.IsSharedWithPatient,
                UpdatedAt = note.UpdatedAt
            };
        }

        public ClinicNoteEditorViewModel BuildEditorModel(
            ClinicPatientAccessContext access,
            ClinicPatientCompactViewModel patient,
            ClinicalNote? note = null,
            ClinicNoteInputViewModel? input = null)
        {
            return new ClinicNoteEditorViewModel
            {
                ClinicId = access.ClinicId,
                ClinicName = access.ClinicName,
                Patient = patient,
                NoteId = note?.Id,
                IsNew = note == null,
                NoteType = input?.NoteType ?? note?.NoteType ?? "general",
                Title = input?.Title ?? note?.Title ?? string.Empty,
                Content = input?.Content ?? note?.Content ?? string.Empty,
                IsPrivate = input?.IsPrivate ?? note == null || !note.IsSharedWithPatient
            };
        }

        public static string BuildPreview(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var normalized = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return normalized.Length <= 140 ? normalized : normalized[..140] + "...";
        }

        private static string NormalizeNoteType(string? noteType)
        {
            var normalized = string.IsNullOrWhiteSpace(noteType) ? "general" : noteType.Trim();
            return normalized.Length > 40 ? normalized[..40] : normalized;
        }

        private static string? NormalizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var normalized = title.Trim();
            return normalized.Length > 200 ? normalized[..200] : normalized;
        }

        private static string NormalizeContent(string? content)
        {
            var normalized = content?.Trim() ?? string.Empty;
            return normalized.Length > 4000 ? normalized[..4000] : normalized;
        }
    }
}
