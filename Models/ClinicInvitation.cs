using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class ClinicInvitation
    {
        public int Id { get; set; }

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string InvitationType { get; set; } = "patient";

        [MaxLength(64)]
        public string? TargetRole { get; set; }

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "pending";

        [Required]
        public string InvitedByUserId { get; set; } = string.Empty;
        public ApplicationUser InvitedBy { get; set; } = null!;

        public string? AcceptedByUserId { get; set; }
        public ApplicationUser? AcceptedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
