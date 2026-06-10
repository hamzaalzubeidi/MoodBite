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
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AchievementService _achievementService;
        private readonly TranslationService _t;

        public CommunityController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AchievementService achievementService, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _achievementService = achievementService;
            _t = t;
        }

        public async Task<IActionResult> Index(string? search, string? dietType)
        {
            var query = _db.CommunityRecipes
                .Include(r => r.User)
                .Include(r => r.RecipeLikes)
                .Where(r => r.IsApproved);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.TitleAr.Contains(search) || r.TitleEn.Contains(search));

            if (!string.IsNullOrEmpty(dietType))
                query = query.Where(r => r.DietType == dietType);

            var recipes = await query.OrderByDescending(r => r.Likes).ToListAsync();
            var userId = _userManager.GetUserId(User);
            var savedIds = await _db.RecipeSaves.Where(s => s.UserId == userId).Select(s => s.RecipeId).ToListAsync();
            var likedIds = await _db.RecipeLikes.Where(l => l.UserId == userId).Select(l => l.RecipeId).ToListAsync();

            var authorIds = recipes.Select(r => r.UserId).Distinct();
            var badges = await _achievementService.GetTopBadgesForUsersAsync(authorIds);

            ViewBag.Search = search;
            ViewBag.DietType = dietType;
            ViewBag.SavedIds = savedIds;
            ViewBag.LikedIds = likedIds;
            ViewBag.UserId = userId;
            ViewBag.Badges = badges;

            return View(recipes);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var recipe = await _db.CommunityRecipes
                .Include(r => r.User)
                .Include(r => r.RecipeLikes)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsApproved);

            if (recipe == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            ViewBag.IsLiked = await _db.RecipeLikes.AnyAsync(l => l.RecipeId == id && l.UserId == userId);
            ViewBag.IsSaved = await _db.RecipeSaves.AnyAsync(s => s.RecipeId == id && s.UserId == userId);

            try
            {
                ViewBag.Ingredients = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(recipe.IngredientsJson);
                ViewBag.Steps = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(recipe.StepsJson);
            }
            catch
            {
                ViewBag.Ingredients = new List<object>();
                ViewBag.Steps = new List<object>();
            }

            return View(recipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int recipeId)
        {
            var userId = _userManager.GetUserId(User)!;
            var existing = await _db.RecipeLikes.FirstOrDefaultAsync(l => l.RecipeId == recipeId && l.UserId == userId);
            var recipe = await _db.CommunityRecipes.FindAsync(recipeId);
            if (recipe == null) return NotFound();

            if (existing == null)
            {
                _db.RecipeLikes.Add(new RecipeLike { RecipeId = recipeId, UserId = userId });
                recipe.Likes++;
            }
            else
            {
                _db.RecipeLikes.Remove(existing);
                recipe.Likes = Math.Max(0, recipe.Likes - 1);
            }
            await _db.SaveChangesAsync();
            return Json(new { likes = recipe.Likes });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int recipeId)
        {
            var userId = _userManager.GetUserId(User)!;
            var existing = await _db.RecipeSaves.FirstOrDefaultAsync(s => s.RecipeId == recipeId && s.UserId == userId);

            if (existing == null)
                _db.RecipeSaves.Add(new RecipeSave { RecipeId = recipeId, UserId = userId });
            else
                _db.RecipeSaves.Remove(existing);

            await _db.SaveChangesAsync();
            return Json(new { saved = existing == null });
        }

        [HttpGet]
        public IActionResult Submit() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            string titleAr, string titleEn, string dietType,
            string ingredientsJson, string stepsJson,
            double calories, double protein, double carbs, double fats, int prepTime)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            titleAr = titleAr?.Trim() ?? "";
            titleEn = titleEn?.Trim() ?? "";
            dietType = dietType?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(titleAr) ||
                string.IsNullOrWhiteSpace(titleEn) ||
                !AllowedDietTypes.Contains(dietType) ||
                !IsInRange(calories, 0, 5000) ||
                !IsInRange(protein, 0, 500) ||
                !IsInRange(carbs, 0, 1000) ||
                !IsInRange(fats, 0, 500) ||
                prepTime < 1 || prepTime > 300 ||
                !HasRecipeItems(ingredientsJson, "name", "nameAr") ||
                !HasRecipeItems(stepsJson, "en", "ar"))
            {
                TempData["Error"] = _t.Get("common.error");
                return View();
            }

            _db.CommunityRecipes.Add(new CommunityRecipe
            {
                UserId = user.Id,
                TitleAr = titleAr,
                TitleEn = titleEn,
                DietType = dietType,
                IngredientsJson = ingredientsJson,
                StepsJson = stepsJson,
                Calories = calories,
                Protein = protein,
                Carbs = carbs,
                Fats = fats,
                PrepTime = prepTime,
                IsApproved = false
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = _t.Get("community.submitted");
            return RedirectToAction("Index");
        }

        private static bool IsInRange(double value, double min, double max) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= min && value <= max;

        private static bool HasRecipeItems(string json, params string[] textKeys)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 20000)
                return false;

            try
            {
                var items = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
                if (items == null || items.Count == 0 || items.Count > 100)
                    return false;

                return items.Any(item => textKeys.Any(key =>
                    item.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)));
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static readonly HashSet<string> AllowedDietTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "keto",
            "mediterranean",
            "vegan",
            "paleo",
            "dash",
            "flexitarian",
            "carnivore",
            "general"
        };
    }
}
