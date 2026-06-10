using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class MealPlan
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string PlanType { get; set; } = "standard"; // standard / ai
        public string PlanJson { get; set; } = "{}";
        public string? EditHistoryJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Title { get; set; }
        public double CalorieTarget { get; set; }

        [MaxLength(100)]
        public string? DietType { get; set; }
    }
}
