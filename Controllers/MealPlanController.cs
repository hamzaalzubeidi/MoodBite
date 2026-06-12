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
    public class MealPlanController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MealPlanService _mealPlanService;
        private readonly GeminiService _geminiService;
        private readonly TranslationService _t;

        public MealPlanController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            MealPlanService mealPlanService,
            GeminiService geminiService,
            TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _mealPlanService = mealPlanService;
            _geminiService = geminiService;
            _t = t;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var standardPlan = await _db.MealPlans
                .Where(m => m.UserId == user.Id && m.PlanType == "standard")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (standardPlan == null)
            {
                var dietSlug = profile?.DietSlug ?? "mediterranean";
                var planJson = _mealPlanService.GenerateWeekPlan(dietSlug);
                standardPlan = new MealPlan
                {
                    UserId     = user.Id,
                    PlanType   = "standard",
                    PlanJson   = planJson,
                    DietType   = dietSlug,
                    CalorieTarget = profile?.CalorieTarget ?? 2000
                };
                _db.MealPlans.Add(standardPlan);
                await _db.SaveChangesAsync();
            }

            var aiPlan = await _db.MealPlans
                .Where(m => m.UserId == user.Id && m.PlanType == "ai")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.Profile      = profile;
            ViewBag.StandardPlan = standardPlan;
            ViewBag.AiPlan       = aiPlan;

            try { ViewBag.StandardDays = JsonSerializer.Deserialize<JsonElement>(standardPlan.PlanJson); }
            catch { ViewBag.StandardDays = null; }

            if (aiPlan != null)
            {
                try { ViewBag.AiDays = JsonSerializer.Deserialize<JsonElement>(aiPlan.PlanJson); }
                catch { ViewBag.AiDays = null; }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regenerate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile  = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var dietSlug = profile?.DietSlug ?? "mediterranean";
            var planJson = _mealPlanService.GenerateWeekPlan(dietSlug, DateTime.Now.Millisecond);

            var plan = new MealPlan
            {
                UserId        = user.Id,
                PlanType      = "standard",
                PlanJson      = planJson,
                DietType      = dietSlug,
                CalorieTarget = profile?.CalorieTarget ?? 2000
            };
            _db.MealPlans.Add(plan);
            await _db.SaveChangesAsync();

            TempData["Success"] = _t.Get("mealPlan.planGenerated");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAI()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                TempData["Error"] = _t.Get("common.profileRequired");
                return RedirectToAction("Index");
            }

            try
            {
                var dietSlug = profile.DietSlug ?? "mediterranean";
                var planJson = await _geminiService.GenerateMealPlanAsync(profile, dietSlug, _t.CurrentLang);
                planJson = CleanJson(planJson);
                if (!IsValidPlanJson(planJson))
                {
                    throw new InvalidOperationException("AI returned invalid meal plan JSON.");
                }

                var title = _t.CurrentLang == "ar"
                    ? $"خطة {dietSlug} — {DateTime.Today:yyyy/MM/dd}"
                    : $"{dietSlug} plan — {DateTime.Today:MMM d, yyyy}";

                var plan = new MealPlan
                {
                    UserId        = user.Id,
                    PlanType      = "ai",
                    PlanJson      = planJson,
                    Title         = title,
                    DietType      = dietSlug,
                    CalorieTarget = profile.CalorieTarget
                };
                _db.MealPlans.Add(plan);
                await _db.SaveChangesAsync();

                TempData["Success"] = _t.Get("mealPlan.aiGenerated");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("الانتظار"))
            {
                TempData["Error"] = ex.Message;
            }
            catch
            {
                var dietSlug = profile.DietSlug ?? "mediterranean";
                var fallbackJson = _mealPlanService.GenerateWeekPlan(dietSlug, DateTime.Now.Millisecond);
                _db.MealPlans.Add(new MealPlan
                {
                    UserId = user.Id,
                    PlanType = "standard",
                    PlanJson = fallbackJson,
                    Title = _t.CurrentLang == "ar"
                        ? $"خطة بديلة — {DateTime.Today:yyyy/MM/dd}"
                        : $"Fallback plan — {DateTime.Today:MMM d, yyyy}",
                    DietType = dietSlug,
                    CalorieTarget = profile.CalorieTarget
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = _t.Get("mealPlan.aiFallback");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAIPlan(string instruction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var aiPlan = await _db.MealPlans
                .Where(m => m.UserId == user.Id && m.PlanType == "ai")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (aiPlan == null)
                return Json(new { success = false, error = "لا توجد خطة ذكاء اصطناعي / No AI plan found" });

            try
            {
                var updatedJson = await _geminiService.EditMealPlanAsync(aiPlan.PlanJson, instruction, _t.CurrentLang);
                updatedJson = CleanJson(updatedJson);

                var historyList = new List<object>();
                if (!string.IsNullOrEmpty(aiPlan.EditHistoryJson))
                {
                    try { historyList = JsonSerializer.Deserialize<List<object>>(aiPlan.EditHistoryJson) ?? []; } catch { }
                }
                historyList.Add(new { instruction, timestamp = DateTime.UtcNow, prevPlan = aiPlan.PlanJson });

                var newPlan = new MealPlan
                {
                    UserId          = user.Id,
                    PlanType        = "ai",
                    PlanJson        = updatedJson,
                    Title           = aiPlan.Title,
                    DietType        = aiPlan.DietType,
                    CalorieTarget   = aiPlan.CalorieTarget,
                    EditHistoryJson = JsonSerializer.Serialize(historyList)
                };
                _db.MealPlans.Add(newPlan);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("الانتظار"))
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch
            {
                return Json(new { success = false, error = _t.Get("mealPlan.aiError") });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShoppingList()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var plan = await _db.MealPlans
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            var shopping = _mealPlanService.GenerateShoppingList(plan?.PlanJson ?? "{}");

            try { ViewBag.Shopping = JsonSerializer.Deserialize<JsonElement>(shopping); }
            catch { ViewBag.Shopping = null; }

            return View();
        }

        // ── Meal Plan History ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var plans = await _db.MealPlans
                .Where(m => m.UserId == user.Id && m.PlanType == "ai")
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(plans);
        }

        [HttpGet]
        public async Task<IActionResult> Load(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var plan = await _db.MealPlans.FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);
            if (plan == null) return Json(new { success = false, error = "الخطة غير موجودة" });

            return Json(new
            {
                success      = true,
                planJson     = plan.PlanJson,
                title        = plan.Title,
                dietType     = plan.DietType,
                calorieTarget= plan.CalorieTarget,
                createdAt    = plan.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var plan = await _db.MealPlans.FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);
            if (plan == null) return Json(new { success = false, error = "الخطة غير موجودة" });

            _db.MealPlans.Remove(plan);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string CleanJson(string json)
        {
            json = json.Trim();
            if (json.StartsWith("```json")) json = json[7..];
            else if (json.StartsWith("```")) json = json[3..];
            if (json.EndsWith("```")) json = json[..^3];
            return json.Trim();
        }

        private static bool IsValidPlanJson(string planJson)
        {
            if (string.IsNullOrWhiteSpace(planJson))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(planJson);
                var root = doc.RootElement;
                return root.ValueKind == JsonValueKind.Array ||
                       (root.ValueKind == JsonValueKind.Object &&
                        root.TryGetProperty("days", out var days) &&
                        days.ValueKind == JsonValueKind.Array);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
