using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using System.Text.Json;

namespace MoodBite.Controllers
{
    [Authorize]
    public class WorkoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GeminiService _geminiService;
        private readonly TranslationService _translationService;

        public WorkoutController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, GeminiService geminiService, TranslationService translationService)
        {
            _db = db;
            _userManager = userManager;
            _geminiService = geminiService;
            _translationService = translationService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var latestPlan = await _db.WorkoutPlans
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.GeneratedAt)
                .FirstOrDefaultAsync();

            ViewBag.Profile = profile;

            if (latestPlan != null)
            {
                try { ViewBag.Plan = JsonSerializer.Deserialize<JsonElement>(latestPlan.PlanJson); }
                catch { ViewBag.Plan = null; }
            }

            return View();
        }

        [HttpGet]
        public IActionResult Questionnaire()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            string? fitnessLevel, string? equipment, string? workoutDuration,
            string? focusArea, int? daysPerWeek)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                TempData["Error"] = _translationService.Get("common.profileRequired");
                return RedirectToAction("Index");
            }

            try
            {
                var planJson = await _geminiService.GenerateWorkoutPlanAsync(
                    profile, fitnessLevel, equipment, workoutDuration, focusArea, daysPerWeek,
                    _translationService.CurrentLang);
                planJson = CleanJson(planJson);

                _db.WorkoutPlans.Add(new WorkoutPlan { UserId = user.Id, PlanJson = planJson });
                await _db.SaveChangesAsync();
                TempData["Success"] = _translationService.Get("workout.planGenerated");
            }
            catch
            {
                var fallbackPlan = BuildFallbackWorkoutPlan(
                    _translationService.CurrentLang,
                    fitnessLevel,
                    equipment,
                    workoutDuration,
                    focusArea,
                    daysPerWeek);
                _db.WorkoutPlans.Add(new WorkoutPlan { UserId = user.Id, PlanJson = fallbackPlan });
                await _db.SaveChangesAsync();
                TempData["Success"] = _translationService.Get("workout.planFallback");
            }

            return RedirectToAction("Index");
        }

        private static string CleanJson(string json)
        {
            json = json.Trim();
            if (json.StartsWith("```json")) json = json[7..];
            else if (json.StartsWith("```")) json = json[3..];
            if (json.EndsWith("```")) json = json[..^3];
            return json.Trim();
        }

        private static string BuildFallbackWorkoutPlan(
            string lang,
            string? fitnessLevel,
            string? equipment,
            string? workoutDuration,
            string? focusArea,
            int? daysPerWeek)
        {
            var duration = string.IsNullOrWhiteSpace(workoutDuration) ? "35 min" : workoutDuration.Trim();
            var level = string.IsNullOrWhiteSpace(fitnessLevel) ? "beginner" : fitnessLevel.Trim();
            var availableEquipment = string.IsNullOrWhiteSpace(equipment) ? "bodyweight" : equipment.Trim();
            var focus = string.IsNullOrWhiteSpace(focusArea) ? "full body" : focusArea.Trim();
            var trainingDays = Math.Clamp(daysPerWeek ?? 4, 2, 6);
            var restDayIndexes = Enumerable.Range(trainingDays + 1, 7 - trainingDays).ToHashSet();

            var days = Enumerable.Range(1, 7).Select(day =>
            {
                var isRest = restDayIndexes.Contains(day);
                return new
                {
                    day,
                    dayNameAr = new[] { "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت", "الأحد" }[day - 1],
                    dayNameEn = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }[day - 1],
                    focus = isRest ? "Rest" : focus,
                    focusAr = isRest ? "راحة" : "تمارين للجسم كامل",
                    isRest,
                    duration = isRest ? "" : duration,
                    warmup = isRest ? "" : "5 min light cardio and dynamic mobility",
                    exercises = isRest
                        ? Array.Empty<object>()
                        : new object[]
                        {
                            new { name = "Squat", nameAr = "قرفصاء", sets = 3, reps = "10-12", rest = "60s", notes = "Keep movement controlled" },
                            new { name = "Push-up", nameAr = "ضغط", sets = 3, reps = "8-12", rest = "60s", notes = "Use incline if needed" },
                            new { name = "Hip hinge", nameAr = "ثني الورك", sets = 3, reps = "10-12", rest = "60s", notes = "Keep back neutral" },
                            new { name = "Row or band pull", nameAr = "سحب مطاط أو تجديف", sets = 3, reps = "10-12", rest = "60s", notes = $"Use {availableEquipment}" },
                            new { name = "Plank", nameAr = "بلانك", sets = 3, reps = "20-40s", rest = "45s", notes = "Breathe steadily" }
                        },
                    cooldown = isRest ? "" : "5 min easy walking and stretching"
                };
            });

            var planName = lang == "ar" ? "خطة تمارين بديلة" : "Fallback workout plan";
            var description = lang == "ar"
                ? "خطة محلية آمنة للاستخدام عند عدم توفر الذكاء الاصطناعي."
                : "A safe local plan used when AI generation is unavailable.";

            return JsonSerializer.Serialize(new
            {
                planName,
                description,
                frequency = $"{trainingDays} days/week",
                level,
                equipment = availableEquipment,
                days
            });
        }
    }
}
