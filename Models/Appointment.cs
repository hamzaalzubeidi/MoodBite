using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        public string PatientId { get; set; } = string.Empty;
        public ApplicationUser Patient { get; set; } = null!;

        [Required]
        public string DietitianId { get; set; } = string.Empty;
        public ApplicationUser Dietitian { get; set; } = null!;

        public DateTime StartsAt { get; set; }
        public int DurationMinutes { get; set; } = 30;

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "scheduled";

        [Required]
        [MaxLength(40)]
        public string VisitType { get; set; } = "followUp";

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
    }
}
