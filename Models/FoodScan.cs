using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class FoodScan
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string ImagePath { get; set; } = string.Empty;

        public string FoodNameAr { get; set; } = string.Empty;
        public string FoodNameEn { get; set; } = string.Empty;

        public int Confidence { get; set; }

        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }

        public string ServingSize { get; set; } = string.Empty;
        public string ServingSizeAr { get; set; } = string.Empty;

        public string? DescriptionAr { get; set; }
        public string? DescriptionEn { get; set; }

        public string? AlternativesJson { get; set; }

        public bool LoggedToDashboard { get; set; }

        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    }
}
