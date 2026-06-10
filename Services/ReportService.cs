using MoodBite.Data;
using MoodBite.Models;
using Microsoft.EntityFrameworkCore;

namespace MoodBite.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ReportData> GetWeeklyReportAsync(string userId, double calorieTarget, int waterGoal = 8)
        {
            var sevenDaysAgo = DateTime.Today.AddDays(-7);
            var logs = await _db.DayLogs
                .Where(d => d.UserId == userId && d.Date >= sevenDaysAgo)
                .OrderBy(d => d.Date)
                .ToListAsync();

            var waterLogs = await _db.WaterLogs
                .Where(w => w.UserId == userId && w.Date >= sevenDaysAgo)
                .OrderBy(w => w.Date)
                .ToListAsync();

            if (logs.Count < 2)
                return new ReportData { HasEnoughData = false };

            var adherentDays = logs.Count(l => l.Adherent);
            var adherencePercent = logs.Count > 0 ? (adherentDays * 100.0 / logs.Count) : 0;
            var avgCalories = logs.Count > 0 ? logs.Average(l => l.CaloriesConsumed) : 0;

            // Trend calculation (compare first half vs second half)
            double trend = 0;
            if (logs.Count >= 4)
            {
                var firstHalf = logs.Take(logs.Count / 2).Average(l => l.CaloriesConsumed);
                var secondHalf = logs.Skip(logs.Count / 2).Average(l => l.CaloriesConsumed);
                trend = firstHalf > 0 ? ((secondHalf - firstHalf) / firstHalf) * 100 : 0;
            }

            var topMood = logs
                .Where(l => !string.IsNullOrEmpty(l.Mood))
                .GroupBy(l => l.Mood)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "";

            var moodFrequency = logs
                .Where(l => !string.IsNullOrEmpty(l.Mood))
                .GroupBy(l => l.Mood)
                .ToDictionary(g => g.Key!, g => g.Count());

            var avgProtein = logs.Any() ? logs.Average(l => l.Protein) : 0;
            var avgCarbs = logs.Any() ? logs.Average(l => l.Carbs) : 0;
            var avgFats = logs.Any() ? logs.Average(l => l.Fats) : 0;

            // Nutritional gap analysis
            var proteinTarget = Math.Round(calorieTarget * 0.30 / 4); // 30% of calories / 4 cal per gram
            var carbsTarget   = Math.Round(calorieTarget * 0.40 / 4); // 40% carbs
            var fatsTarget    = Math.Round(calorieTarget * 0.30 / 9); // 30% fats / 9 cal per gram

            var dailyCalories = logs.Select(l => new DailyCalorieData
            {
                Date = l.Date.ToString("MM/dd"),
                Calories = (int)l.CaloriesConsumed,
                Target = (int)calorieTarget,
                IsAdherent = l.Adherent
            }).ToList();

            // Build 7-day water series (pad missing days with 0)
            var today = DateTime.Today;
            var dailyWater = new List<DailyWaterData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var wl = waterLogs.FirstOrDefault(w => w.Date.Date == date.Date);
                dailyWater.Add(new DailyWaterData
                {
                    Date = date.ToString("MM/dd"),
                    Glasses = wl?.GlassesCount ?? 0,
                    Goal = waterGoal
                });
            }

            var avgWater = waterLogs.Any() ? waterLogs.Average(w => w.GlassesCount) : 0;

            return new ReportData
            {
                HasEnoughData = true,
                AdherencePercent = Math.Round(adherencePercent, 1),
                AvgCalories = Math.Round(avgCalories, 0),
                CalorieTarget = calorieTarget,
                Trend = Math.Round(trend, 1),
                TopMood = topMood,
                MoodFrequency = moodFrequency,
                AvgProtein = Math.Round(avgProtein, 1),
                AvgCarbs = Math.Round(avgCarbs, 1),
                AvgFats = Math.Round(avgFats, 1),
                ProteinTarget = proteinTarget,
                CarbsTarget = carbsTarget,
                FatsTarget = fatsTarget,
                DailyCalories = dailyCalories,
                DaysLogged = logs.Count,
                DailyWater = dailyWater,
                AvgWater = Math.Round(avgWater, 1),
                WaterGoal = waterGoal
            };
        }
    }

    public class ReportData
    {
        public bool HasEnoughData { get; set; }
        public double AdherencePercent { get; set; }
        public double AvgCalories { get; set; }
        public double CalorieTarget { get; set; }
        public double Trend { get; set; }
        public string TopMood { get; set; } = string.Empty;
        public Dictionary<string, int> MoodFrequency { get; set; } = new();
        public double AvgProtein { get; set; }
        public double AvgCarbs { get; set; }
        public double AvgFats { get; set; }
        public double ProteinTarget { get; set; }
        public double CarbsTarget { get; set; }
        public double FatsTarget { get; set; }
        public List<DailyCalorieData> DailyCalories { get; set; } = new();
        public int DaysLogged { get; set; }
        public List<DailyWaterData> DailyWater { get; set; } = new();
        public double AvgWater { get; set; }
        public int WaterGoal { get; set; }
    }

    public class DailyWaterData
    {
        public string Date { get; set; } = string.Empty;
        public int Glasses { get; set; }
        public int Goal { get; set; }
    }

    public class DailyCalorieData
    {
        public string Date { get; set; } = string.Empty;
        public int Calories { get; set; }
        public int Target { get; set; }
        public bool IsAdherent { get; set; }
    }
}
