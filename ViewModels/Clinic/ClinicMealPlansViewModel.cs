namespace MoodBite.ViewModels.Clinic
{
    public class ClinicMealPlansIndexViewModel
    {
        public bool HasClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public int TotalPlans { get; set; }
        public int PatientsWithPlans { get; set; }
        public List<ClinicMealPlanListItemViewModel> RecentPlans { get; set; } = new();
    }

    public class ClinicMealPlanPatientViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicMealPlanPatientSummaryViewModel Patient { get; set; } = new();
        public List<ClinicMealPlanListItemViewModel> Plans { get; set; } = new();
    }

    public class ClinicMealPlanEditorViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicMealPlanPatientSummaryViewModel Patient { get; set; } = new();
        public int? PlanId { get; set; }
        public bool IsNew { get; set; }
        public bool IsLatestAssigned { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PlanType { get; set; } = "standard";
        public string? DietType { get; set; }
        public double CalorieTarget { get; set; }
        public string PlanJson { get; set; } = "{}";
        public List<ClinicMealPlanDayViewModel> Days { get; set; } = new();
    }

    public class ClinicMealPlanDetailsViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicMealPlanPatientSummaryViewModel Patient { get; set; } = new();
        public ClinicMealPlanListItemViewModel Plan { get; set; } = new();
        public string PlanJson { get; set; } = "{}";
        public List<ClinicMealPlanDayViewModel> Days { get; set; } = new();
    }

    public class ClinicMealPlanInputViewModel
    {
        public int ClinicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PlanType { get; set; } = "standard";
        public string? DietType { get; set; }
        public double CalorieTarget { get; set; }
        public string PlanJson { get; set; } = "{}";
        public string SubmitAction { get; set; } = "manual";
    }

    public class ClinicMealPlanListItemViewModel
    {
        public int Id { get; set; }
        public int ClinicId { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? PatientEmail { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public string? DietType { get; set; }
        public double CalorieTarget { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsLatestAssigned { get; set; }
        public int DayCount { get; set; }
    }

    public class ClinicMealPlanPatientSummaryViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Goal { get; set; }
        public string? DietSlug { get; set; }
        public double? LatestWeight { get; set; }
        public double? Bmi { get; set; }
        public double? CalorieTarget { get; set; }
        public int WaterGoal { get; set; } = 8;
    }

    public class ClinicMealPlanDayViewModel
    {
        public int Day { get; set; }
        public string? DayNameAr { get; set; }
        public string? DayNameEn { get; set; }
        public double TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalFats { get; set; }
        public List<ClinicMealPlanMealViewModel> Meals { get; set; } = new();
    }

    public class ClinicMealPlanMealViewModel
    {
        public string Type { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
    }
}
