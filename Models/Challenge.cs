using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class Challenge
    {
        public int Id { get; set; }

        [Required]
        public string Slug { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string DietType { get; set; } = string.Empty;
        public string TasksJson { get; set; } = "[]";
        public string DescriptionAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;

        public ICollection<UserChallenge> UserChallenges { get; set; } = new List<UserChallenge>();
    }

    public class UserChallenge
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int ChallengeId { get; set; }
        public Challenge Challenge { get; set; } = null!;

        public int CurrentDay { get; set; }
        public int Streak { get; set; }
        public DateTime StartDate { get; set; }
        public string CompletedDaysJson { get; set; } = "[]";
        public DateTime? LastCheckIn { get; set; }
    }
}
