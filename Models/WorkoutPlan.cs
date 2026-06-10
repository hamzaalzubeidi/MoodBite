using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class WorkoutPlan
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string PlanJson { get; set; } = "{}";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
