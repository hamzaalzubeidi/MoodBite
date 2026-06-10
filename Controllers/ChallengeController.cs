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
    public class ChallengeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TranslationService _t;

        public ChallengeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _t = t;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var allChallenges = await _db.Challenges.ToListAsync();
            var myChallenges = await _db.UserChallenges
                .Include(uc => uc.Challenge)
                .Where(uc => uc.UserId == user.Id)
                .ToListAsync();

            var myIds = myChallenges.Select(uc => uc.ChallengeId).ToHashSet();

            // Leaderboard: top users by streak sum
            // ExpandoObject is used so dynamic access works across Razor's runtime-compiled assembly.
            var rawLeaderboard = await _db.UserChallenges
                .Include(uc => uc.User)
                .GroupBy(uc => uc.UserId)
                .Select(g => new { UserId = g.Key, UserName = g.First().User.FullName, TotalStreak = g.Sum(x => x.Streak), TotalDays = g.Sum(x => x.CurrentDay) })
                .OrderByDescending(x => x.TotalStreak)
                .Take(7)
                .ToListAsync();
            var leaderboard = rawLeaderboard.Select(x => {
                dynamic entry = new System.Dynamic.ExpandoObject();
                entry.UserId = x.UserId;
                entry.UserName = x.UserName ?? string.Empty;
                entry.TotalStreak = x.TotalStreak;
                entry.TotalDays = x.TotalDays;
                return (dynamic)entry;
            }).ToList();

            ViewBag.AllChallenges = allChallenges;
            ViewBag.MyChallenges = myChallenges;
            ViewBag.JoinedIds = myIds;
            ViewBag.Leaderboard = leaderboard;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int challengeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _db.UserChallenges.AnyAsync(uc => uc.UserId == user.Id && uc.ChallengeId == challengeId);
            if (!existing)
            {
                _db.UserChallenges.Add(new UserChallenge
                {
                    UserId = user.Id,
                    ChallengeId = challengeId,
                    StartDate = DateTime.Today,
                    CurrentDay = 0,
                    Streak = 0,
                    CompletedDaysJson = "[]"
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = _t.Get("challenge.joined");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTask(int userChallengeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var uc = await _db.UserChallenges.FirstOrDefaultAsync(x => x.Id == userChallengeId && x.UserId == user.Id);
            if (uc == null) return NotFound();

            var today = DateTime.Today;
            if (uc.LastCheckIn?.Date == today)
            {
                TempData["Error"] = _t.Get("challenge.alreadyCompleted");
                return RedirectToAction("Index");
            }

            var completedDays = JsonSerializer.Deserialize<List<string>>(uc.CompletedDaysJson) ?? [];
            completedDays.Add(today.ToString("yyyy-MM-dd"));
            uc.CompletedDaysJson = JsonSerializer.Serialize(completedDays);
            var previousCheckIn = uc.LastCheckIn?.Date;
            uc.CurrentDay = Math.Min(uc.CurrentDay + 1, 30);

            // Streak: check if yesterday was checked in
            var yesterday = today.AddDays(-1);
            if (previousCheckIn == yesterday.Date) uc.Streak++;
            else uc.Streak = 1;
            uc.LastCheckIn = today;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"أحسنت! اليوم {uc.CurrentDay}/30 / Day {uc.CurrentDay}/30 completed!";
            return RedirectToAction("Index");
        }
    }
}
