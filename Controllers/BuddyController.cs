using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;

namespace MoodBite.Controllers
{
    [Authorize]
    public class BuddyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AchievementService _achievementService;
        private readonly TranslationService _t;

        public BuddyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AchievementService achievementService, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _achievementService = achievementService;
            _t = t;
        }

        public async Task<IActionResult> Index(string? goal)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Find potential buddies (users with accepted requests or browsable)
            var acceptedBuddyIds = await _db.BuddyRequests
                .Where(r => (r.SenderId == user.Id || r.ReceiverId == user.Id) && r.Status == "accepted")
                .Select(r => r.SenderId == user.Id ? r.ReceiverId : r.SenderId)
                .ToListAsync();

            var pendingSentIds = await _db.BuddyRequests
                .Where(r => r.SenderId == user.Id && r.Status == "pending")
                .Select(r => r.ReceiverId)
                .ToListAsync();

            // Potential buddies = other users not already buddies
            var potentialQueryBase = _db.Users
                .Include(u => u.HealthProfile)
                .Where(u => u.Id != user.Id && u.IsActive && !acceptedBuddyIds.Contains(u.Id));

            if (!string.IsNullOrEmpty(goal))
                potentialQueryBase = potentialQueryBase.Where(u => u.HealthProfile != null && u.HealthProfile.Goal == goal);

            var potential = await potentialQueryBase.Take(20).ToListAsync();

            // My buddies with stats
            var myBuddies = await _db.Users
                .Where(u => acceptedBuddyIds.Contains(u.Id))
                .Include(u => u.HealthProfile)
                .Include(u => u.DayLogs)
                .ToListAsync();

            // Incoming requests
            var requests = await _db.BuddyRequests
                .Where(r => r.ReceiverId == user.Id && r.Status == "pending")
                .Include(r => r.Sender).ThenInclude(s => s.HealthProfile)
                .ToListAsync();

            var allUserIds = potential.Select(u => u.Id)
                .Concat(myBuddies.Select(u => u.Id))
                .Concat(requests.Select(r => r.Sender.Id))
                .Distinct();
            var badges = await _achievementService.GetTopBadgesForUsersAsync(allUserIds);

            ViewBag.Potential = potential;
            ViewBag.MyBuddies = myBuddies;
            ViewBag.Requests = requests;
            ViewBag.PendingSentIds = pendingSentIds;
            ViewBag.UserProfile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            ViewBag.GoalFilter = goal;
            ViewBag.Badges = badges;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string receiverId, string? message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _db.BuddyRequests.AnyAsync(r =>
                r.SenderId == user.Id && r.ReceiverId == receiverId);
            if (!existing)
            {
                _db.BuddyRequests.Add(new BuddyRequest
                {
                    SenderId = user.Id,
                    ReceiverId = receiverId,
                    Status = "pending",
                    Message = message
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = _t.Get("buddy.requestSent");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _db.BuddyRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == user!.Id);
            if (req != null)
            {
                req.Status = "accepted";
                await _db.SaveChangesAsync();
                TempData["Success"] = _t.Get("buddy.requestAccepted");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _db.BuddyRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == user!.Id);
            if (req != null)
            {
                req.Status = "declined";
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
