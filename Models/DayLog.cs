using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class DayLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public DateTime Date { get; set; }
        public double CaloriesConsumed { get; set; }
        public double CaloriesBurned { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public string? Mood { get; set; }
        public bool Adherent { get; set; }
    }
}
