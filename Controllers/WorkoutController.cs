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
            catch (Exception ex)
            {
                TempData["Error"] = _translationService.Get("common.error") + ": " + ex.Message;
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
    }
}
