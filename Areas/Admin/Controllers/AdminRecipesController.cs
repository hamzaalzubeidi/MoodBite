using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Services;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminRecipesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly TranslationService _t;

        public AdminRecipesController(ApplicationDbContext db, TranslationService t) { _db = db; _t = t; }

        public async Task<IActionResult> Index(bool? pending)
        {
            var query = _db.CommunityRecipes.Include(r => r.User).AsQueryable();
            if (pending == true) query = query.Where(r => !r.IsApproved);
            var recipes = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.ShowPending = pending;
            return View(recipes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var recipe = await _db.CommunityRecipes.FindAsync(id);
            if (recipe != null) { recipe.IsApproved = true; await _db.SaveChangesAsync(); }
            TempData["Success"] = _t.Get("admin.recipeApproved");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var recipe = await _db.CommunityRecipes.FindAsync(id);
            if (recipe != null) { _db.CommunityRecipes.Remove(recipe); await _db.SaveChangesAsync(); }
            TempData["Success"] = _t.Get("admin.recipeRejected");
            return RedirectToAction("Index");
        }
    }
}
