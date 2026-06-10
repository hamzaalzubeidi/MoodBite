using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class ClinicalNote
    {
        public int Id { get; set; }

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = string.Empty;
        public ApplicationUser Patient { get; set; } = null!;

        [Required]
        public string AuthorId { get; set; } = string.Empty;
        public ApplicationUser Author { get; set; } = null!;

        [Required]
        [MaxLength(40)]
        public string NoteType { get; set; } = "general";

        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        public bool IsSharedWithPatient { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
