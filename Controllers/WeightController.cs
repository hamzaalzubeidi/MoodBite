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
    public class WeightController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TranslationService _t;

        public WeightController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _t = t;
        }

        // GET /Weight
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var logs = await _db.WeightLogs
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            ViewBag.Profile = profile;

            return View(logs);
        }

        // POST /Weight/Log
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Log(double weight, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (weight <= 0 || weight > 500)
            {
                TempData["Error"] = _t.Get("weight.invalidWeight");
                return RedirectToAction("Index");
            }

            var today = DateTime.Today;
            var existing = await _db.WeightLogs
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.Date.Date == today);

            if (existing != null)
            {
                existing.Weight = weight;
                existing.Note   = note?.Trim();
            }
            else
            {
                _db.WeightLogs.Add(new WeightLog
                {
                    UserId = user.Id,
                    Date   = today,
                    Weight = weight,
                    Note   = note?.Trim()
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = _t.Get("weight.saved");
            return RedirectToAction("Index");
        }

        // DELETE /Weight/{id}  (AJAX)
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var log = await _db.WeightLogs.FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);
            if (log == null) return Json(new { success = false, error = "السجل غير موجود" });

            _db.WeightLogs.Remove(log);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // GET /Weight/ChartData  (JSON for Chart.js)
        [HttpGet]
        public async Task<IActionResult> ChartData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var logs = await _db.WeightLogs
                .Where(w => w.UserId == user.Id)
                .OrderBy(w => w.Date)
                .TakeLast(30)
                .ToListAsync();

            return Json(new
            {
                labels = logs.Select(l => l.Date.ToString("MM/dd")).ToArray(),
                data   = logs.Select(l => l.Weight).ToArray()
            });
        }
    }
}
