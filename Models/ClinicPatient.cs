using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class ClinicPatient
    {
        public int Id { get; set; }

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = string.Empty;
        public ApplicationUser Patient { get; set; } = null!;

        public string? PrimaryDietitianId { get; set; }
        public ApplicationUser? PrimaryDietitian { get; set; }

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "active";

        public bool ConsentGranted { get; set; }
        public DateTime? ConsentGrantedAt { get; set; }
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }

        [MaxLength(500)]
        public string? InternalNotes { get; set; }
    }
}
