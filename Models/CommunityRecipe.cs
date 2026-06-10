using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class CommunityRecipe
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string TitleAr { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string DietType { get; set; } = string.Empty;
        public string IngredientsJson { get; set; } = "[]";
        public string StepsJson { get; set; } = "[]";
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public int PrepTime { get; set; }
        public int Likes { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RecipeLike> RecipeLikes { get; set; } = new List<RecipeLike>();
        public ICollection<RecipeSave> RecipeSaves { get; set; } = new List<RecipeSave>();
    }

    public class RecipeLike
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public CommunityRecipe Recipe { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
    }

    public class RecipeSave
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public CommunityRecipe Recipe { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
    }
}
