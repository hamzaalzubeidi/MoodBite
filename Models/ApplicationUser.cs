using Microsoft.AspNetCore.Identity;

namespace MoodBite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string PreferredLanguage { get; set; } = "ar";

        public HealthProfile? HealthProfile { get; set; }
        public ICollection<DayLog> DayLogs { get; set; } = new List<DayLog>();
        public ICollection<WeightLog> WeightLogs { get; set; } = new List<WeightLog>();
        public ICollection<WaterLog> WaterLogs { get; set; } = new List<WaterLog>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
        public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
        public ICollection<UserChallenge> UserChallenges { get; set; } = new List<UserChallenge>();
        public ICollection<CommunityRecipe> CommunityRecipes { get; set; } = new List<CommunityRecipe>();
        public ICollection<BuddyRequest> SentBuddyRequests { get; set; } = new List<BuddyRequest>();
        public ICollection<BuddyRequest> ReceivedBuddyRequests { get; set; } = new List<BuddyRequest>();
        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
        public ICollection<FoodScan> FoodScans { get; set; } = new List<FoodScan>();
        public ICollection<BodyProgress> BodyProgressEntries { get; set; } = new List<BodyProgress>();
        public ICollection<ClinicMember> ClinicMemberships { get; set; } = new List<ClinicMember>();
        public ICollection<ClinicMember> InvitedClinicMembers { get; set; } = new List<ClinicMember>();
        public ICollection<ClinicPatient> ClinicPatientLinks { get; set; } = new List<ClinicPatient>();
        public ICollection<ClinicPatient> AssignedClinicPatients { get; set; } = new List<ClinicPatient>();
        public ICollection<ClinicInvitation> SentClinicInvitations { get; set; } = new List<ClinicInvitation>();
        public ICollection<ClinicInvitation> AcceptedClinicInvitations { get; set; } = new List<ClinicInvitation>();
        public ICollection<ClinicalNote> PatientClinicalNotes { get; set; } = new List<ClinicalNote>();
        public ICollection<ClinicalNote> AuthoredClinicalNotes { get; set; } = new List<ClinicalNote>();
        public ICollection<Appointment> PatientAppointments { get; set; } = new List<Appointment>();
        public ICollection<Appointment> DietitianAppointments { get; set; } = new List<Appointment>();
    }
}
