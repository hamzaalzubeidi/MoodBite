using MoodBite.Models;

namespace MoodBite.ViewModels
{
    public class DashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public HealthProfile? Profile { get; set; }
        public DayLog? TodayLog { get; set; }
        public double? TodayWeight { get; set; }
        public List<DayLog> Last7Logs { get; set; } = new();
        public List<WeightLog> WeightLogs { get; set; } = new();
        public int Streak { get; set; }
        public double AdherencePercent { get; set; }
        public double AvgCalories { get; set; }
        public int DaysLogged { get; set; }
        public string BestDay { get; set; } = string.Empty;
        public double CalorieTarget { get; set; }
        public int TodayGlasses { get; set; }
        public int WaterGoal { get; set; }
        public BodyProgress? LatestBodyProgress { get; set; }
        public int DaysUntilNextMeasurement { get; set; }
    }
}
