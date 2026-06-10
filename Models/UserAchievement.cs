using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class UserAchievement
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string AchievementKey { get; set; } = string.Empty;

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
}
