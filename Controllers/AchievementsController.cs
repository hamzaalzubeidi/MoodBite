using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodBite.Models;
using MoodBite.Services;

namespace MoodBite.Controllers
{
    [Authorize]
    public class AchievementsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AchievementService _achievementService;
        private readonly TranslationService _t;

        public AchievementsController(UserManager<ApplicationUser> userManager, AchievementService achievementService, TranslationService t)
        {
            _userManager = userManager;
            _achievementService = achievementService;
            _t = t;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var (achievements, totalPoints, possiblePoints) = await _achievementService.GetPageDataAsync(user.Id);

            var filtered = string.IsNullOrEmpty(category)
                ? achievements
                : achievements.Where(a => a.Definition.Category == category).ToList();

            ViewBag.Category       = category;
            ViewBag.TotalPoints    = totalPoints;
            ViewBag.PossiblePoints = possiblePoints;
            ViewBag.UnlockedCount  = achievements.Count(a => a.IsUnlocked);
            ViewBag.TotalCount     = achievements.Count;

            return View(filtered);
        }

        [HttpGet]
        public async Task<IActionResult> CheckNew()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { keys = Array.Empty<string>() });

            var newKeys = await _achievementService.CheckAndUnlockAsync(user.Id);

            var isRtl = _t.CurrentLang == "ar";
            var details = newKeys.Select(k =>
            {
                var def = AchievementService.All.FirstOrDefault(d => d.Key == k);
                return def == null ? null : new
                {
                    key   = k,
                    name  = isRtl ? def.NameAr : def.NameEn,
                    emoji = def.Emoji,
                    pts   = def.Points
                };
            }).Where(x => x != null).ToList();

            return Json(new { keys = details });
        }
    }
}
