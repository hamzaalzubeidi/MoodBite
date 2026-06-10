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
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TranslationService _t;

        public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _t = t;
        }

        public async Task<IActionResult> Index(string? type)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var query = _db.Notifications.Where(n => n.UserId == user.Id);
            if (!string.IsNullOrEmpty(type)) query = query.Where(n => n.Type == type);

            var notifications = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
            ViewBag.SelectedType = type;
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user!.Id);
            if (notif != null) { notif.IsRead = true; await _db.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            var notifs = await _db.Notifications.Where(n => n.UserId == user!.Id && !n.IsRead).ToListAsync();
            notifs.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearRead()
        {
            var user = await _userManager.GetUserAsync(User);
            var read = await _db.Notifications.Where(n => n.UserId == user!.Id && n.IsRead).ToListAsync();
            _db.Notifications.RemoveRange(read);
            await _db.SaveChangesAsync();
            TempData["Success"] = _t.Get("notifications.cleared");
            return RedirectToAction("Index");
        }
    }
}
