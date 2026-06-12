using MoodBite.Services;

namespace MoodBite.ViewModels.Clinic
{
    public class ClinicPatientsIndexViewModel
    {
        public bool HasClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? Query { get; set; }
        public string StatusFilter { get; set; } = "all";
        public string ConsentFilter { get; set; } = "all";
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int PendingPatients { get; set; }
        public int PendingInvitations { get; set; }
        public string? LastInvitationLink { get; set; }
        public List<ClinicPatientRosterItemViewModel> Patients { get; set; } = new();
        public List<ClinicPatientInvitationItemViewModel> Invitations { get; set; } = new();
    }

    public class ClinicPatientRosterItemViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ConsentGranted { get; set; }
        public DateTime LinkedAt { get; set; }
        public string? PrimaryDietitianName { get; set; }
        public string? DietSlug { get; set; }
        public string? Goal { get; set; }
        public double? Weight { get; set; }
        public double? CalorieTarget { get; set; }
        public DateTime? ProfileUpdatedAt { get; set; }
    }

    public class ClinicPatientInvitationItemViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? InvitedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }

    public class ClinicInvitePatientViewModel
    {
        public int ClinicId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class ClinicLinkExistingPatientViewModel
    {
        public int ClinicId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class ClinicAcceptInvitationViewModel
    {
        public string Token { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public bool IsValid { get; set; }
        public bool IsAccepted { get; set; }
        public string? ClinicName { get; set; }
        public string? Email { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Message { get; set; }
    }

    public class ClinicPatientDetailsViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ConsentGranted { get; set; }
        public DateTime? ConsentGrantedAt { get; set; }
        public DateTime LinkedAt { get; set; }
        public string? PrimaryDietitianName { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public double? Height { get; set; }
        public double? ProfileWeight { get; set; }
        public double? CurrentWeight { get; set; }
        public double? WeightChange { get; set; }
        public double? Bmi { get; set; }
        public string? Goal { get; set; }
        public string? DietSlug { get; set; }
        public double? CalorieTarget { get; set; }
        public int WaterTarget { get; set; } = 8;
        public DateTime? ProfileUpdatedAt { get; set; }
        public ClinicPatientMealPlanSummaryViewModel MealPlan { get; set; } = new();
        public string? RecentActivityKey { get; set; }
        public DateTime? RecentActivityAt { get; set; }
        public List<ClinicPatientWeightLogItemViewModel> WeightHistory { get; set; } = new();
        public List<ClinicPatientDayLogItemViewModel> MealLogs { get; set; } = new();
        public List<ClinicPatientFoodScanItemViewModel> FoodScans { get; set; } = new();
        public List<ClinicPatientWaterLogItemViewModel> WaterLogs { get; set; } = new();
        public int TodayWaterGlasses { get; set; }
        public int TodayWaterProgressPercent { get; set; }
        public List<ClinicPatientProgressItemViewModel> ProgressEntries { get; set; } = new();
        public List<ClinicNoteListItemViewModel> ClinicalNotes { get; set; } = new();
        public List<ClinicAppointmentListItemViewModel> UpcomingAppointments { get; set; } = new();
        public ReportData WeeklyReport { get; set; } = new();
        public string WeightTrendKey { get; set; } = "clinic.patientDashboard.trend.insufficient";
        public int RecentMealLogCount { get; set; }
        public int RecentFoodScanCount { get; set; }
        public double RecentAvgCalories { get; set; }
        public double RecentAvgProtein { get; set; }
        public double RecentAvgCarbs { get; set; }
        public double RecentAvgFats { get; set; }
    }

    public class ClinicPatientMealPlanSummaryViewModel
    {
        public int? PlanId { get; set; }
        public bool HasPlan { get; set; }
        public string? Title { get; set; }
        public string? PlanType { get; set; }
        public string? DietType { get; set; }
        public double? CalorieTarget { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ClinicPatientWeightLogItemViewModel
    {
        public DateTime Date { get; set; }
        public double Weight { get; set; }
        public double? ChangeFromPrevious { get; set; }
        public string? Note { get; set; }
    }

    public class ClinicPatientDayLogItemViewModel
    {
        public DateTime Date { get; set; }
        public double CaloriesConsumed { get; set; }
        public double CaloriesBurned { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public string? Mood { get; set; }
        public bool Adherent { get; set; }
    }

    public class ClinicPatientFoodScanItemViewModel
    {
        public string FoodName { get; set; } = string.Empty;
        public int Confidence { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public string? ServingSize { get; set; }
        public bool LoggedToDashboard { get; set; }
        public DateTime ScannedAt { get; set; }
    }

    public class ClinicPatientWaterLogItemViewModel
    {
        public DateTime Date { get; set; }
        public int GlassesCount { get; set; }
        public int Goal { get; set; }
        public int ProgressPercent { get; set; }
    }

    public class ClinicPatientProgressItemViewModel
    {
        public DateTime Date { get; set; }
        public double? Weight { get; set; }
        public double? Waist { get; set; }
        public double? Hips { get; set; }
        public double? Chest { get; set; }
        public double? Arms { get; set; }
        public string? Notes { get; set; }
        public string? PhotoPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
