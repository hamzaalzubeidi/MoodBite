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
    public class DietsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly TranslationService _t;
        private readonly UserManager<ApplicationUser> _userManager;

        public DietsController(ApplicationDbContext db, TranslationService t, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _t = t;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? category, string? difficulty)
        {
            var query = _db.Diets.Where(d => d.IsActive);
            if (!string.IsNullOrEmpty(category)) query = query.Where(d => d.Category == category);
            if (!string.IsNullOrEmpty(difficulty)) query = query.Where(d => d.Difficulty == difficulty);

            var diets = await query.ToListAsync();
            ViewBag.Category = category;
            ViewBag.Difficulty = difficulty;

            // Load current user's selected diet (null if not logged in)
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    ViewBag.SelectedDietSlug = profile?.DietSlug;
                }
            }

            return View(diets);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectDiet(string slug)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                TempData["Error"] = _t.Get("common.profileRequired");
                return RedirectToAction("Index");
            }

            profile.DietSlug = slug;
            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var diet = await _db.Diets.FirstOrDefaultAsync(d => d.Slug == slug);
            var dietName = _t.CurrentLang == "ar" ? diet?.NameAr : diet?.NameEn;
            TempData["Success"] = _t.CurrentLang == "ar"
                ? $"تم اختيار نظام {dietName} بنجاح"
                : $"{dietName} diet selected successfully";

            return RedirectToAction("Index");
        }

        [HttpGet("Diets/Detail/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            var diet = await _db.Diets.FirstOrDefaultAsync(d => d.Slug == slug && d.IsActive);
            if (diet == null) return NotFound();

            try
            {
                var lang = _t.CurrentLang;
                var benefits = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(diet.BenefitsJson);
                var foods = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(diet.FoodsJson);
                var sampleMeals = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(diet.SampleMealsJson);
                var tips = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(diet.TipsJson);

                ViewBag.Benefits = benefits?[lang] ?? benefits?["en"] ?? [];
                ViewBag.Foods = foods ?? [];
                ViewBag.SampleMeals = sampleMeals ?? [];
                ViewBag.Tips = tips?[lang] ?? tips?["en"] ?? [];
            }
            catch
            {
                ViewBag.Benefits = new List<string>();
                ViewBag.Foods = new List<object>();
                ViewBag.SampleMeals = new List<object>();
                ViewBag.Tips = new List<string>();
            }

            return View(diet);
        }
    }
}
