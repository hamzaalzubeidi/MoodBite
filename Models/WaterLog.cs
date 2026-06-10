using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class WaterLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public DateTime Date { get; set; }
        public int GlassesCount { get; set; }
    }
}
