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
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TranslationService _t;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _t = t;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user!.Id);
            ViewBag.Profile = profile;
            ViewBag.Step = 1;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            int age, string gender, double height, double weight,
            string goal, string activityLevel,
            string[]? healthConditions, string[]? allergens,
            string[]? foodPreferences, string cookingStyle, string budget, int waterGoal = 8)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var dietSlug = RecommendDiet(goal, healthConditions ?? [], allergens ?? []);
            var calorieTarget = CalculateCalorieTarget(age, gender, height, weight, goal, activityLevel);

            var existing = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (existing != null)
            {
                existing.Age = age;
                existing.Gender = gender;
                existing.Height = height;
                existing.Weight = weight;
                existing.Goal = goal;
                existing.ActivityLevel = activityLevel;
                existing.HealthConditions = JsonSerializer.Serialize(healthConditions ?? []);
                existing.Allergens = JsonSerializer.Serialize(allergens ?? []);
                existing.FoodPreferences = JsonSerializer.Serialize(foodPreferences ?? []);
                existing.CookingStyle = cookingStyle;
                existing.Budget = budget;
                existing.WaterGoal = waterGoal > 0 ? waterGoal : 8;
                existing.DietSlug = dietSlug;
                existing.CalorieTarget = calorieTarget;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.HealthProfiles.Add(new HealthProfile
                {
                    UserId = user.Id,
                    Age = age,
                    Gender = gender,
                    Height = height,
                    Weight = weight,
                    Goal = goal,
                    ActivityLevel = activityLevel,
                    HealthConditions = JsonSerializer.Serialize(healthConditions ?? []),
                    Allergens = JsonSerializer.Serialize(allergens ?? []),
                    FoodPreferences = JsonSerializer.Serialize(foodPreferences ?? []),
                    CookingStyle = cookingStyle,
                    Budget = budget,
                    WaterGoal = waterGoal > 0 ? waterGoal : 8,
                    DietSlug = dietSlug,
                    CalorieTarget = calorieTarget,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Result");
        }

        public async Task<IActionResult> Result()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user!.Id);
            if (profile == null) return RedirectToAction("Index");

            var diet = await _db.Diets.FirstOrDefaultAsync(d => d.Slug == profile.DietSlug);
            ViewBag.Diet = diet;
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile picture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || picture == null || picture.Length == 0)
                return RedirectToAction("Index");

            var ext = Path.GetExtension(picture.FileName).ToLower();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
            {
                TempData["Error"] = _t.Get("profile.unsupportedFile");
                return RedirectToAction("Index");
            }

            var fileName = $"{user.Id}_{DateTime.UtcNow.Ticks}{ext}";
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsPath);
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await picture.CopyToAsync(stream);

            user.ProfilePicture = fileName;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = _t.Get("profile.picUpdated");
            return RedirectToAction("Index");
        }

        private static double CalculateCalorieTarget(int age, string gender, double height, double weight, string goal, string activityLevel)
        {
            double bmr = gender == "male"
                ? 10 * weight + 6.25 * height - 5 * age + 5
                : 10 * weight + 6.25 * height - 5 * age - 161;

            double multiplier = activityLevel switch
            {
                "sedentary" => 1.2,
                "lightly-active" => 1.375,
                "moderately-active" => 1.55,
                "very-active" => 1.725,
                "extremely-active" => 1.9,
                _ => 1.2
            };

            double tdee = bmr * multiplier;
            return goal switch
            {
                "lose-weight" => tdee - 500,
                "build-muscle" => tdee + 300,
                _ => tdee
            };
        }

        private static string RecommendDiet(string goal, string[] conditions, string[] allergens)
        {
            bool hasHeartDisease = conditions.Contains("heart-disease") || conditions.Contains("hypertension");
            bool hasDiabetes = conditions.Contains("diabetes");
            bool hasDairy = allergens.Contains("dairy");
            bool hasGluten = allergens.Contains("gluten");

            if (hasHeartDisease) return "dash";
            if (hasDiabetes) return "mediterranean";
            if (goal == "lose-weight") return "keto";
            if (goal == "build-muscle") return "paleo";
            if (hasGluten || hasDairy) return "vegan";
            if (goal == "improve-health") return "mediterranean";
            return "mediterranean";
        }
    }
}
