using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string? ActorUserId { get; set; }

        [MaxLength(256)]
        public string? ActorEmail { get; set; }

        [MaxLength(256)]
        public string? ActorRoles { get; set; }

        public int? ClinicId { get; set; }

        [MaxLength(450)]
        public string? TargetUserId { get; set; }

        [MaxLength(80)]
        public string TargetEntityType { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? TargetEntityId { get; set; }

        [MaxLength(120)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Summary { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(2000)]
        public string? MetadataJson { get; set; }
    }
}

