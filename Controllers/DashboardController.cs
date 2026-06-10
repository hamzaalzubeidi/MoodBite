using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using MoodBite.ViewModels;
using System.Text.Json;

namespace MoodBite.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly TranslationService _t;

        public DashboardController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
            _t = t;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var today = DateTime.Today;
            var todayLog = await _db.DayLogs.FirstOrDefaultAsync(d => d.UserId == user.Id && d.Date.Date == today);
            var todayWeight = await _db.WeightLogs
                .Where(w => w.UserId == user.Id && w.Date.Date == today)
                .FirstOrDefaultAsync();

            // Last 7 day logs
            var sevenDaysAgo = today.AddDays(-6);
            var recentLogs = await _db.DayLogs
                .Where(d => d.UserId == user.Id && d.Date.Date >= sevenDaysAgo)
                .OrderBy(d => d.Date)
                .ToListAsync();

            // Pad to 7 days
            var last7Logs = new List<DayLog>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var log = recentLogs.FirstOrDefault(l => l.Date.Date == date.Date);
                last7Logs.Add(log ?? new DayLog { Date = date, CaloriesConsumed = 0 });
            }

            // Weight logs for chart
            var weightLogs = await _db.WeightLogs
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.Date)
                .Take(14)
                .OrderBy(w => w.Date)
                .ToListAsync();

            // Streak calculation
            int streak = 0;
            var checkDate = today;
            while (true)
            {
                var hasLog = await _db.DayLogs.AnyAsync(d => d.UserId == user.Id && d.Date.Date == checkDate.Date);
                if (!hasLog) break;
                streak++;
                checkDate = checkDate.AddDays(-1);
            }

            // Weekly stats
            double adherencePercent = 0;
            double avgCalories = 0;
            string bestDay = "";
            int daysLogged = recentLogs.Count;
            if (daysLogged > 0)
            {
                var adherentDays = recentLogs.Count(l => l.Adherent);
                adherencePercent = Math.Round(adherentDays * 100.0 / daysLogged, 1);
                avgCalories = Math.Round(recentLogs.Average(l => l.CaloriesConsumed), 0);
                var best = recentLogs.OrderByDescending(l => l.Adherent ? 1 : 0).FirstOrDefault();
                bestDay = best?.Date.ToString("ddd") ?? "";
            }

            // Today's water log
            var todayWater = await _db.WaterLogs
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.Date.Date == today);

            // Latest body progress
            var latestBodyProgress = await _db.BodyProgressEntries
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.Date)
                .FirstOrDefaultAsync();
            int daysUntilNext = 7;
            if (latestBodyProgress != null)
            {
                var daysSinceLast = (today - latestBodyProgress.Date.Date).Days;
                daysUntilNext = Math.Max(0, 7 - daysSinceLast);
            }

            // Create default notifications if first time
            var notifCount = await _db.Notifications.CountAsync(n => n.UserId == user.Id);
            if (notifCount == 0)
                await _notificationService.CreateDefaultNotificationsAsync(user.Id);

            // Sunday progress reminder
            await _notificationService.EnsureSundayProgressReminderAsync(user.Id);

            // Load current diet for display in dashboard
            if (!string.IsNullOrEmpty(profile?.DietSlug))
                ViewBag.CurrentDiet = await _db.Diets.FirstOrDefaultAsync(d => d.Slug == profile.DietSlug);

            var vm = new DashboardViewModel
            {
                User = user,
                Profile = profile,
                TodayLog = todayLog,
                TodayWeight = todayWeight?.Weight,
                Last7Logs = last7Logs,
                WeightLogs = weightLogs,
                Streak = streak,
                AdherencePercent = adherencePercent,
                AvgCalories = avgCalories,
                DaysLogged = daysLogged,
                BestDay = bestDay,
                CalorieTarget = profile?.CalorieTarget ?? 2000,
                TodayGlasses = todayWater?.GlassesCount ?? 0,
                WaterGoal = profile?.WaterGoal > 0 ? profile.WaterGoal : 8,
                LatestBodyProgress = latestBodyProgress,
                DaysUntilNextMeasurement = daysUntilNext
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogCalories(double caloriesConsumed, double caloriesBurned,
            double protein, double carbs, double fats)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!IsInRange(caloriesConsumed, 0, 20000) ||
                !IsInRange(caloriesBurned, 0, 10000) ||
                !IsInRange(protein, 0, 2000) ||
                !IsInRange(carbs, 0, 3000) ||
                !IsInRange(fats, 0, 2000))
            {
                TempData["Error"] = _t.Get("common.error");
                return RedirectToAction("Index");
            }

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var target = profile?.CalorieTarget ?? 2000;
            var net = caloriesConsumed - caloriesBurned;
            var adherent = Math.Abs(net - target) / target <= 0.15;

            var today = DateTime.Today;
            var existing = await _db.DayLogs.FirstOrDefaultAsync(d => d.UserId == user.Id && d.Date.Date == today);

            if (existing != null)
            {
                existing.CaloriesConsumed = caloriesConsumed;
                existing.CaloriesBurned = caloriesBurned;
                existing.Protein = protein;
                existing.Carbs = carbs;
                existing.Fats = fats;
                existing.Adherent = adherent;
            }
            else
            {
                _db.DayLogs.Add(new DayLog
                {
                    UserId = user.Id,
                    Date = today,
                    CaloriesConsumed = caloriesConsumed,
                    CaloriesBurned = caloriesBurned,
                    Protein = protein,
                    Carbs = carbs,
                    Fats = fats,
                    Adherent = adherent
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = _t.Get("dashboard.caloriesLogged");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogWeight(double weight)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!IsInRange(weight, 20, 500))
            {
                TempData["Error"] = _t.Get("weight.invalidWeight");
                return RedirectToAction("Index");
            }

            var today = DateTime.Today;
            var existing = await _db.WeightLogs.FirstOrDefaultAsync(w => w.UserId == user.Id && w.Date.Date == today);

            if (existing != null) existing.Weight = weight;
            else _db.WeightLogs.Add(new WeightLog { UserId = user.Id, Date = today, Weight = weight });

            await _db.SaveChangesAsync();
            TempData["Success"] = _t.Get("dashboard.weightLogged");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMood(string mood)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!AllowedMoods.Contains(mood))
            {
                TempData["Error"] = _t.Get("common.error");
                return RedirectToAction("Index");
            }

            var today = DateTime.Today;
            var log = await _db.DayLogs.FirstOrDefaultAsync(d => d.UserId == user.Id && d.Date.Date == today);

            if (log != null)
            {
                log.Mood = mood;
                await _db.SaveChangesAsync();
            }
            else
            {
                _db.DayLogs.Add(new DayLog
                {
                    UserId = user.Id,
                    Date = today,
                    Mood = mood
                });
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = _t.Get("dashboard.moodLogged");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> TodayWater()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { glasses = 0, goal = 8 });

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var goal    = profile?.WaterGoal > 0 ? profile.WaterGoal : 8;
            var today   = DateTime.Today;
            var log     = await _db.WaterLogs.FirstOrDefaultAsync(w => w.UserId == user.Id && w.Date.Date == today);

            return Json(new { glasses = log?.GlassesCount ?? 0, goal });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogWater(int delta)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            if (delta != -1 && delta != 1)
                return BadRequest(new { success = false });

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var goal = profile?.WaterGoal > 0 ? profile.WaterGoal : 8;

            var today = DateTime.Today;
            var existing = await _db.WaterLogs
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.Date.Date == today);

            int newCount;
            if (existing != null)
            {
                newCount = Math.Max(0, Math.Min(existing.GlassesCount + delta, goal * 2));
                existing.GlassesCount = newCount;
            }
            else
            {
                newCount = Math.Max(0, Math.Min(delta, goal * 2));
                _db.WaterLogs.Add(new WaterLog
                {
                    UserId = user.Id,
                    Date = today,
                    GlassesCount = newCount
                });
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, glasses = newCount, goal });
        }

        private static bool IsInRange(double value, double min, double max) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= min && value <= max;

        private static readonly HashSet<string> AllowedMoods = new(StringComparer.OrdinalIgnoreCase)
        {
            "tired",
            "stressed",
            "needEnergy",
            "veryHungry",
            "cantSleep",
            "postWorkout",
            "sick",
            "great"
        };
    }
}
