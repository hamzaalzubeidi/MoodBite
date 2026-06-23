using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminDietsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditLogService _audit;

        public AdminDietsController(ApplicationDbContext db, IAuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var diets = await _db.Diets.ToListAsync();
            return View(diets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var diet = await _db.Diets.FindAsync(id);
            if (diet != null)
            {
                diet.IsActive = !diet.IsActive;
                await _db.SaveChangesAsync();
                await _audit.LogAsync(
                    diet.IsActive ? "admin.diets.activated" : "admin.diets.deactivated",
                    "Diet",
                    diet.Id.ToString(),
                    summary: diet.IsActive ? "Admin activated diet." : "Admin deactivated diet.",
                    metadata: new { diet.Slug, diet.IsActive });
            }
            return RedirectToAction("Index");
        }
    }
}
