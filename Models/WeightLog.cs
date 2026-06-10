using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class WeightLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public DateTime Date { get; set; }
        public double Weight { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
