using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class BodyProgress
    {
        public int Id { get; set; }
        [Required] public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public DateTime Date { get; set; } = DateTime.Today;
        public double? Weight { get; set; }
        public double? Waist { get; set; }
        public double? Hips { get; set; }
        public double? Chest { get; set; }
        public double? Arms { get; set; }
        public string? Notes { get; set; }
        public string? PhotoPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
