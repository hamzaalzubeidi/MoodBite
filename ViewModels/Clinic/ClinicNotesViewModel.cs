namespace MoodBite.ViewModels.Clinic
{
    public class ClinicNotesIndexViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicPatientCompactViewModel Patient { get; set; } = new();
        public List<ClinicNoteListItemViewModel> Notes { get; set; } = new();
    }

    public class ClinicNoteEditorViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicPatientCompactViewModel Patient { get; set; } = new();
        public int? NoteId { get; set; }
        public bool IsNew { get; set; }
        public string NoteType { get; set; } = "general";
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = true;
    }

    public class ClinicNoteDetailsViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicPatientCompactViewModel Patient { get; set; } = new();
        public ClinicNoteListItemViewModel Note { get; set; } = new();
        public string Content { get; set; } = string.Empty;
        public string NoteType { get; set; } = "general";
        public bool IsPrivate { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ClinicNoteInputViewModel
    {
        public int ClinicId { get; set; }
        public string NoteType { get; set; } = "general";
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = true;
    }

    public class ClinicNoteListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsPrivate { get; set; }
    }

    public class ClinicPatientCompactViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Goal { get; set; }
        public string? DietSlug { get; set; }
    }
}
