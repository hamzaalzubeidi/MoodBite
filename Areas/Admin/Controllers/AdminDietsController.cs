using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminDietsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminDietsController(ApplicationDbContext db) => _db = db;

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
            if (diet != null) { diet.IsActive = !diet.IsActive; await _db.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }
    }
}
