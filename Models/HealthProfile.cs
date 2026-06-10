using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class HealthProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Weight { get; set; }
        public string Goal { get; set; } = string.Empty;
        public string ActivityLevel { get; set; } = string.Empty;
        public string? HealthConditions { get; set; }  // JSON array
        public string? Allergens { get; set; }          // JSON array
        public string? FoodPreferences { get; set; }    // JSON array
        public string CookingStyle { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string? DietSlug { get; set; }
        public double CalorieTarget { get; set; }
        public int WaterGoal { get; set; } = 8;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
