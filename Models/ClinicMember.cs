using System.ComponentModel.DataAnnotations;
using MoodBite.Constants;

namespace MoodBite.Models
{
    public class ClinicMember
    {
        public int Id { get; set; }

        public int ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(64)]
        public string Role { get; set; } = ApplicationRoles.Dietitian;

        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public string? InvitedByUserId { get; set; }
        public ApplicationUser? InvitedBy { get; set; }
    }
}
