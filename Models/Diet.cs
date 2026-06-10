using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class Diet
    {
        public int Id { get; set; }

        [Required]
        public string Slug { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string ColorGradient { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public int CalorieMin { get; set; }
        public int CalorieMax { get; set; }
        public string BenefitsJson { get; set; } = "{}";
        public string FoodsJson { get; set; } = "[]";
        public string SampleMealsJson { get; set; } = "[]";
        public string TipsJson { get; set; } = "{}";
        public bool IsActive { get; set; } = true;
    }
}
